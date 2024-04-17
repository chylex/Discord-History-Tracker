using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Server.Download;
using DHT.Utils.Logging;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Repositories;

sealed class SqliteDownloadRepository : BaseSqliteRepository, IDownloadRepository {
	private static readonly Log Log = Log.ForType<SqliteDownloadRepository>();
	
	private readonly SqliteConnectionPool pool;

	public SqliteDownloadRepository(SqliteConnectionPool pool) : base(Log) {
		this.pool = pool;
	}

	internal sealed class NewDownloadCollector : IAsyncDisposable {
		private readonly SqliteDownloadRepository repository;
		private bool hasAdded = false;

		private readonly SqliteCommand metadataCmd;

		public NewDownloadCollector(SqliteDownloadRepository repository, ISqliteConnection conn) {
			this.repository = repository;

			metadataCmd = conn.Command(
				"""
				INSERT INTO download_metadata (normalized_url, download_url, status, type, size)
				VALUES (:normalized_url, :download_url, :status, :type, :size)
				ON CONFLICT DO NOTHING
				"""
			);
			metadataCmd.Add(":normalized_url", SqliteType.Text);
			metadataCmd.Add(":download_url", SqliteType.Text);
			metadataCmd.Add(":status", SqliteType.Integer);
			metadataCmd.Add(":type", SqliteType.Text);
			metadataCmd.Add(":size", SqliteType.Integer);
		}

		public async Task Add(Data.Download download) {
			metadataCmd.Set(":normalized_url", download.NormalizedUrl);
			metadataCmd.Set(":download_url", download.DownloadUrl);
			metadataCmd.Set(":status", (int) download.Status);
			metadataCmd.Set(":type", download.Type);
			metadataCmd.Set(":size", download.Size);
			hasAdded |= await metadataCmd.ExecuteNonQueryAsync() > 0;
		}

		public void OnCommitted() {
			if (hasAdded) {
				repository.UpdateTotalCount();
			}
		}

		public async ValueTask DisposeAsync() {
			await metadataCmd.DisposeAsync();
		}
	}

	public async Task AddDownload(Data.Download item, Stream? stream) {
		await using (var conn = await pool.Take()) {
			await conn.BeginTransactionAsync();
			
			await using var metadataCmd = conn.Upsert("download_metadata", [
				("normalized_url", SqliteType.Text),
				("download_url", SqliteType.Text),
				("status", SqliteType.Integer),
				("type", SqliteType.Text),
				("size", SqliteType.Integer),
			]);

			metadataCmd.Set(":normalized_url", item.NormalizedUrl);
			metadataCmd.Set(":download_url", item.DownloadUrl);
			metadataCmd.Set(":status", (int) item.Status);
			metadataCmd.Set(":type", item.Type);
			metadataCmd.Set(":size", item.Size);
			await metadataCmd.ExecuteNonQueryAsync();

			if (stream == null) {
				await using var deleteBlobCmd = conn.Command("DELETE FROM download_blobs WHERE normalized_url = :normalized_url");
				deleteBlobCmd.AddAndSet(":normalized_url", SqliteType.Text, item.NormalizedUrl);
				await deleteBlobCmd.ExecuteNonQueryAsync();
			}
			else {
				await using var upsertBlobCmd = conn.Command(
					"""
					INSERT INTO download_blobs (normalized_url, blob)
					VALUES (:normalized_url, ZEROBLOB(:blob_length))
					ON CONFLICT (normalized_url) DO UPDATE SET blob = excluded.blob
					RETURNING rowid
					"""
				);
				
				upsertBlobCmd.AddAndSet(":normalized_url", SqliteType.Text, item.NormalizedUrl);
				upsertBlobCmd.AddAndSet(":blob_length", SqliteType.Integer, item.Size);
				long rowid = await upsertBlobCmd.ExecuteLongScalarAsync();

				await using var blob = new SqliteBlob(conn.InnerConnection, "download_blobs", "blob", rowid);
				await stream.CopyToAsync(blob);
			}

			await conn.CommitTransactionAsync();
		}

		UpdateTotalCount();
	}

	public override Task<long> Count(CancellationToken cancellationToken) {
		return Count(filter: null, cancellationToken);
	}

	public async Task<long> Count(DownloadItemFilter? filter, CancellationToken cancellationToken) {
		await using var conn = await pool.Take();
		return await conn.ExecuteReaderAsync("SELECT COUNT(*) FROM download_metadata" + filter.GenerateConditions().BuildWhereClause(), static reader => reader?.GetInt64(0) ?? 0L, cancellationToken);
	}

	public async Task<DownloadStatusStatistics> GetStatistics(DownloadItemFilter nonSkippedFilter, CancellationToken cancellationToken) {
		nonSkippedFilter.IncludeStatuses = null;
		nonSkippedFilter.ExcludeStatuses = null;
		string nonSkippedFilterConditions = nonSkippedFilter.GenerateConditions().Build();

		await using var conn = await pool.Take();

		await using var cmd = conn.Command(
			$"""
			 SELECT
			 IFNULL(SUM(CASE WHEN (status = :downloading) OR (status = :pending AND {nonSkippedFilterConditions}) THEN 1 ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN (status = :downloading) OR (status = :pending AND {nonSkippedFilterConditions}) THEN IFNULL(size, 0) ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN ((status = :downloading) OR (status = :pending AND {nonSkippedFilterConditions})) AND size IS NULL THEN 1 ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN status = :success THEN 1 ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN status = :success THEN IFNULL(size, 0) ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN status = :success AND size IS NULL THEN 1 ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN status NOT IN (:pending, :downloading, :success) THEN 1 ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN status NOT IN (:pending, :downloading, :success) THEN IFNULL(size, 0) ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN status NOT IN (:pending, :downloading, :success) AND size IS NULL THEN 1 ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN status = :pending AND NOT ({nonSkippedFilterConditions}) THEN 1 ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN status = :pending AND NOT ({nonSkippedFilterConditions}) THEN IFNULL(size, 0) ELSE 0 END), 0),
			 IFNULL(SUM(CASE WHEN status = :pending AND NOT ({nonSkippedFilterConditions}) AND size IS NULL THEN 1 ELSE 0 END), 0)
			 FROM download_metadata
			 """
		);

		cmd.AddAndSet(":pending", SqliteType.Integer, (int) DownloadStatus.Pending);
		cmd.AddAndSet(":downloading", SqliteType.Integer, (int) DownloadStatus.Downloading);
		cmd.AddAndSet(":success", SqliteType.Integer, (int) DownloadStatus.Success);

		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

		if (!await reader.ReadAsync(cancellationToken)) {
			return new DownloadStatusStatistics();
		}

		return new DownloadStatusStatistics {
			PendingCount = reader.GetInt32(0),
			PendingTotalSize = reader.GetUint64(1),
			PendingWithUnknownSizeCount = reader.GetInt32(2),
			SuccessfulCount = reader.GetInt32(3),
			SuccessfulTotalSize = reader.GetUint64(4),
			SuccessfulWithUnknownSizeCount = reader.GetInt32(5),
			FailedCount = reader.GetInt32(6),
			FailedTotalSize = reader.GetUint64(7),
			FailedWithUnknownSizeCount = reader.GetInt32(8),
			SkippedCount = reader.GetInt32(9),
			SkippedTotalSize = reader.GetUint64(10),
			SkippedWithUnknownSizeCount = reader.GetInt32(11)
		};
	}

	public async IAsyncEnumerable<Data.Download> Get() {
		await using var conn = await pool.Take();

		await using var cmd = conn.Command("SELECT normalized_url, download_url, status, type, size FROM download_metadata");
		await using var reader = await cmd.ExecuteReaderAsync();

		while (await reader.ReadAsync()) {
			string normalizedUrl = reader.GetString(0);
			string downloadUrl = reader.GetString(1);
			var status = (DownloadStatus) reader.GetInt32(2);
			string? type = reader.IsDBNull(3) ? null : reader.GetString(3);
			ulong? size = reader.IsDBNull(4) ? null : reader.GetUint64(4);

			yield return new Data.Download(normalizedUrl, downloadUrl, status, type, size);
		}
	}

	public async Task<DownloadWithData> HydrateWithData(Data.Download download) {
		await using var conn = await pool.Take();

		await using var cmd = conn.Command("SELECT blob FROM download_blobs WHERE normalized_url = :url");
		cmd.AddAndSet(":url", SqliteType.Text, download.NormalizedUrl);

		await using var reader = await cmd.ExecuteReaderAsync();
		var data = await reader.ReadAsync() && !reader.IsDBNull(0) ? (byte[]) reader["blob"] : null;
		
		return new DownloadWithData(download, data);
	}

	public async Task<bool> GetSuccessfulDownloadWithData(string normalizedUrl, Func<Data.Download, Stream, Task> dataProcessor) {
		await using var conn = await pool.Take();

		await using var cmd = conn.Command(
			"""
			SELECT dm.download_url, dm.type, db.rowid FROM download_metadata dm
			JOIN download_blobs db ON dm.normalized_url = db.normalized_url
			WHERE dm.normalized_url = :normalized_url AND dm.status = :success IS NOT NULL
			"""
		);

		cmd.AddAndSet(":normalized_url", SqliteType.Text, normalizedUrl);
		cmd.AddAndSet(":success", SqliteType.Integer, (int) DownloadStatus.Success);

		string downloadUrl;
		string? type;
		long rowid;
		
		await using (var reader = await cmd.ExecuteReaderAsync()) {
			if (!await reader.ReadAsync()) {
				return false;
			}

			downloadUrl = reader.GetString(0);
			type = reader.IsDBNull(1) ? null : reader.GetString(1);
			rowid = reader.GetInt64(2);
		}
		
		await using (var blob = new SqliteBlob(conn.InnerConnection, "download_blobs", "blob", rowid, readOnly: true)) {
			await dataProcessor(new Data.Download(normalizedUrl, downloadUrl, DownloadStatus.Success, type, (ulong) blob.Length), blob);
		}

		return true;
	}

	public async IAsyncEnumerable<DownloadItem> PullPendingDownloadItems(int count, DownloadItemFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken) {
		filter.IncludeStatuses = [DownloadStatus.Pending];
		filter.ExcludeStatuses = null;

		var found = new List<DownloadItem>();

		await using var conn = await pool.Take();

		var sql = $"""
		           SELECT normalized_url, download_url, type, size
		           FROM download_metadata
		           {filter.GenerateConditions().BuildWhereClause()}
		           LIMIT :limit
		           """;

		await using (var cmd = conn.Command(sql)) {
			cmd.AddAndSet(":limit", SqliteType.Integer, Math.Max(0, count));

			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken)) {
				found.Add(new DownloadItem {
					NormalizedUrl = reader.GetString(0),
					DownloadUrl = reader.GetString(1),
					Type = reader.IsDBNull(2) ? null : reader.GetString(2),
					Size = reader.IsDBNull(3) ? null : reader.GetUint64(3)
				});
			}
		}

		if (found.Count != 0) {
			await using var cmd = conn.Command("UPDATE download_metadata SET status = :downloading WHERE normalized_url = :normalized_url AND status = :pending");
			cmd.AddAndSet(":pending", SqliteType.Integer, (int) DownloadStatus.Pending);
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

	public async Task MoveDownloadingItemsBackToQueue(CancellationToken cancellationToken) {
		await using var conn = await pool.Take();

		await using var cmd = conn.Command("UPDATE download_metadata SET status = :pending WHERE status = :downloading");
		cmd.AddAndSet(":pending", SqliteType.Integer, (int) DownloadStatus.Pending);
		cmd.AddAndSet(":downloading", SqliteType.Integer, (int) DownloadStatus.Downloading);
		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<int> RetryFailed(CancellationToken cancellationToken) {
		await using var conn = await pool.Take();

		await using var cmd = conn.Command("UPDATE download_metadata SET status = :pending WHERE status = :generic_error OR (status > :last_custom_code AND status != :success)");
		cmd.AddAndSet(":pending", SqliteType.Integer, (int) DownloadStatus.Pending);
		cmd.AddAndSet(":generic_error", SqliteType.Integer, (int) DownloadStatus.GenericError);
		cmd.AddAndSet(":last_custom_code", SqliteType.Integer, (int) DownloadStatus.LastCustomCode);
		cmd.AddAndSet(":success", SqliteType.Integer, (int) DownloadStatus.Success);
		return await cmd.ExecuteNonQueryAsync(cancellationToken);
	}
}
