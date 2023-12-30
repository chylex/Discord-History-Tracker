using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Server.Download;
using DHT.Utils.Tasks;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Repositories;

sealed class SqliteDownloadRepository : IDownloadRepository {
	private readonly SqliteConnectionPool pool;
	private readonly AsyncValueComputer<long>.Single totalDownloadsComputer;

	public SqliteDownloadRepository(SqliteConnectionPool pool, AsyncValueComputer<long>.Single totalDownloadsComputer) {
		this.pool = pool;
		this.totalDownloadsComputer = totalDownloadsComputer;
	}

	public async Task<long> CountAttachments(AttachmentFilter? filter, CancellationToken cancellationToken) {
		using var conn = pool.Take();
		return await conn.ExecuteReaderAsync("SELECT COUNT(DISTINCT normalized_url) FROM attachments a" + filter.GenerateWhereClause("a"), static reader => reader?.GetInt64(0) ?? 0L, cancellationToken);
	}

	public async Task AddDownload(Data.Download download) {
		using (var conn = pool.Take()) {
			await using var cmd = conn.Upsert("downloads", [
				("normalized_url", SqliteType.Text),
				("download_url", SqliteType.Text),
				("status", SqliteType.Integer),
				("size", SqliteType.Integer),
				("blob", SqliteType.Blob)
			]);

			cmd.Set(":normalized_url", download.NormalizedUrl);
			cmd.Set(":download_url", download.DownloadUrl);
			cmd.Set(":status", (int) download.Status);
			cmd.Set(":size", download.Size);
			cmd.Set(":blob", download.Data);
			await cmd.ExecuteNonQueryAsync();
		}

		totalDownloadsComputer.Recompute();
	}

	public async Task<DownloadStatusStatistics> GetStatistics(CancellationToken cancellationToken) {
		static async Task LoadUndownloadedStatistics(ISqliteConnection conn, DownloadStatusStatistics result, CancellationToken cancellationToken) {
			await using var cmd = conn.Command(
				"""
				SELECT IFNULL(COUNT(size), 0), IFNULL(SUM(size), 0)
				FROM (SELECT MAX(a.size) size
				      FROM attachments a
				      WHERE a.normalized_url NOT IN (SELECT d.normalized_url FROM downloads d)
				      GROUP BY a.normalized_url)
				""");

			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

			if (reader.Read()) {
				result.SkippedCount = reader.GetInt32(0);
				result.SkippedSize = reader.GetUint64(1);
			}
		}

		static async Task LoadSuccessStatistics(ISqliteConnection conn, DownloadStatusStatistics result, CancellationToken cancellationToken) {
			await using var cmd = conn.Command(
				"""
				SELECT
				IFNULL(SUM(CASE WHEN status IN (:enqueued, :downloading) THEN 1 ELSE 0 END), 0),
				IFNULL(SUM(CASE WHEN status IN (:enqueued, :downloading) THEN size ELSE 0 END), 0),
				IFNULL(SUM(CASE WHEN status = :success THEN 1 ELSE 0 END), 0),
				IFNULL(SUM(CASE WHEN status = :success THEN size ELSE 0 END), 0),
				IFNULL(SUM(CASE WHEN status NOT IN (:enqueued, :downloading) AND status != :success THEN 1 ELSE 0 END), 0),
				IFNULL(SUM(CASE WHEN status NOT IN (:enqueued, :downloading) AND status != :success THEN size ELSE 0 END), 0)
				FROM downloads
				"""
			);
			
			cmd.AddAndSet(":enqueued", SqliteType.Integer, (int) DownloadStatus.Enqueued);
			cmd.AddAndSet(":downloading", SqliteType.Integer, (int) DownloadStatus.Downloading);
			cmd.AddAndSet(":success", SqliteType.Integer, (int) DownloadStatus.Success);

			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

			if (reader.Read()) {
				result.EnqueuedCount = reader.GetInt32(0);
				result.EnqueuedSize = reader.GetUint64(1);
				result.SuccessfulCount = reader.GetInt32(2);
				result.SuccessfulSize = reader.GetUint64(3);
				result.FailedCount = reader.GetInt32(4);
				result.FailedSize = reader.GetUint64(5);
			}
		}

		var result = new DownloadStatusStatistics();

		using var conn = pool.Take();
		await LoadUndownloadedStatistics(conn, result, cancellationToken);
		await LoadSuccessStatistics(conn, result, cancellationToken);
		return result;
	}

	public async IAsyncEnumerable<Data.Download> GetWithoutData() {
		using var conn = pool.Take();

		await using var cmd = conn.Command("SELECT normalized_url, download_url, status, size FROM downloads");
		await using var reader = await cmd.ExecuteReaderAsync();

		while (reader.Read()) {
			string normalizedUrl = reader.GetString(0);
			string downloadUrl = reader.GetString(1);
			var status = (DownloadStatus) reader.GetInt32(2);
			ulong size = reader.GetUint64(3);

			yield return new Data.Download(normalizedUrl, downloadUrl, status, size);
		}
	}

	public async Task<Data.Download> HydrateWithData(Data.Download download) {
		using var conn = pool.Take();

		await using var cmd = conn.Command("SELECT blob FROM downloads WHERE normalized_url = :url");
		cmd.AddAndSet(":url", SqliteType.Text, download.NormalizedUrl);

		await using var reader = await cmd.ExecuteReaderAsync();

		if (reader.Read() && !reader.IsDBNull(0)) {
			return download.WithData((byte[]) reader["blob"]);
		}
		else {
			return download;
		}
	}

	public async Task<DownloadedAttachment?> GetDownloadedAttachment(string normalizedUrl) {
		using var conn = pool.Take();

		await using var cmd = conn.Command(
			"""
			SELECT a.type, d.blob FROM downloads d
			LEFT JOIN attachments a ON d.normalized_url = a.normalized_url
			WHERE d.normalized_url = :normalized_url AND d.status = :success AND d.blob IS NOT NULL
			"""
		);

		cmd.AddAndSet(":normalized_url", SqliteType.Text, normalizedUrl);
		cmd.AddAndSet(":success", SqliteType.Integer, (int) DownloadStatus.Success);

		await using var reader = await cmd.ExecuteReaderAsync();

		if (!reader.Read()) {
			return null;
		}

		return new DownloadedAttachment {
			Type = reader.IsDBNull(0) ? null : reader.GetString(0),
			Data = (byte[]) reader["blob"],
		};
	}

	public async Task<int> EnqueueDownloadItems(AttachmentFilter? filter, CancellationToken cancellationToken) {
		using var conn = pool.Take();

		await using var cmd = conn.Command(
			$"""
			 INSERT INTO downloads (normalized_url, download_url, status, size)
			 SELECT a.normalized_url, a.download_url, :enqueued, MAX(a.size)
			 FROM attachments a
			 {filter.GenerateWhereClause("a")}
			 GROUP BY a.normalized_url
			 """
		);

		cmd.AddAndSet(":enqueued", SqliteType.Integer, (int) DownloadStatus.Enqueued);
		return await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	public async IAsyncEnumerable<DownloadItem> PullEnqueuedDownloadItems(int count, [EnumeratorCancellation] CancellationToken cancellationToken) {
		var found = new List<DownloadItem>();

		using var conn = pool.Take();

		await using (var cmd = conn.Command("SELECT normalized_url, download_url, size FROM downloads WHERE status = :enqueued LIMIT :limit")) {
			cmd.AddAndSet(":enqueued", SqliteType.Integer, (int) DownloadStatus.Enqueued);
			cmd.AddAndSet(":limit", SqliteType.Integer, Math.Max(0, count));

			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

			while (reader.Read()) {
				found.Add(new DownloadItem {
					NormalizedUrl = reader.GetString(0),
					DownloadUrl = reader.GetString(1),
					Size = reader.GetUint64(2),
				});
			}
		}

		if (found.Count != 0) {
			await using var cmd = conn.Command("UPDATE downloads SET status = :downloading WHERE normalized_url = :normalized_url AND status = :enqueued");
			cmd.AddAndSet(":enqueued", SqliteType.Integer, (int) DownloadStatus.Enqueued);
			cmd.AddAndSet(":downloading", SqliteType.Integer, (int) DownloadStatus.Downloading);
			cmd.Add(":normalized_url", SqliteType.Text);

			foreach (var item in found) {
				cmd.Set(":normalized_url", item.NormalizedUrl);

				if (await cmd.ExecuteNonQueryAsync(cancellationToken) == 1) {
					yield return item;
				}
			}
		}
	}

	public async Task RemoveDownloadItems(DownloadItemFilter? filter, FilterRemovalMode mode) {
		using (var conn = pool.Take()) {
			await conn.ExecuteAsync(
				$"""
				 -- noinspection SqlWithoutWhere
				 DELETE FROM downloads
				 {filter.GenerateWhereClause(invert: mode == FilterRemovalMode.KeepMatching)}
				 """
			);
		}

		totalDownloadsComputer.Recompute();
	}
}
