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

sealed class SqliteDownloadRepository(SqliteConnectionPool pool) : BaseSqliteRepository(Log), IDownloadRepository {
	private static readonly Log Log = Log.ForType<SqliteDownloadRepository>();
	
	public const string Schema = "downloads";
	
	internal sealed class NewDownloadCollector : IAsyncDisposable {
		private readonly SqliteDownloadRepository repository;
		private bool hasChanged = false;
		
		private readonly SqliteCommand metadataCmd;
		
		public NewDownloadCollector(SqliteDownloadRepository repository, ISqliteConnection conn) {
			this.repository = repository;
			
			metadataCmd = conn.Command(
				"""
				INSERT INTO download_metadata (normalized_url, download_url, status, type, size)
				VALUES (:normalized_url, :download_url, :status, :type, :size)
				ON CONFLICT (normalized_url)
				DO UPDATE SET
					download_url = excluded.download_url,
					type = IFNULL(excluded.type, type),
					size = IFNULL(excluded.size, size)
				WHERE status != :success
				  AND (download_url != excluded.download_url
				    OR (excluded.type IS NOT NULL AND type IS NOT excluded.type)
				    OR (excluded.size IS NOT NULL AND size IS NOT excluded.size)
				  )
				"""
			);
			metadataCmd.Add(":normalized_url", SqliteType.Text);
			metadataCmd.Add(":download_url", SqliteType.Text);
			metadataCmd.Add(":status", SqliteType.Integer);
			metadataCmd.Add(":type", SqliteType.Text);
			metadataCmd.Add(":size", SqliteType.Integer);
			metadataCmd.AddAndSet(":success", SqliteType.Integer, (int) DownloadStatus.Success);
		}
		
		public async Task Add(Data.Download download) {
			metadataCmd.Set(":normalized_url", download.NormalizedUrl);
			metadataCmd.Set(":download_url", download.DownloadUrl);
			metadataCmd.Set(":status", (int) download.Status);
			metadataCmd.Set(":type", download.Type);
			metadataCmd.Set(":size", download.Size);
			hasChanged |= await metadataCmd.ExecuteNonQueryAsync() > 0;
		}
		
		public Task AddIfNotNull(Data.Download? download) {
			if (download != null) {
				return Add(download);
			}
			else {
				return Task.CompletedTask;
			}
		}
		
		public void OnCommitted() {
			if (hasChanged) {
				repository.UpdateTotalCount();
			}
		}
		
		public async ValueTask DisposeAsync() {
			await metadataCmd.DisposeAsync();
		}
	}
	
	private static SqliteBlob BlobReference(ISqliteConnection conn, long rowid, bool readOnly) {
		string schema = conn.HasAttachedDatabase(Schema) ? Schema : "main";
		return new SqliteBlob(conn.InnerConnection, databaseName: schema, tableName: "download_blobs", columnName: "blob", rowid, readOnly);
	}
	
	public async Task AddDownload(Data.Download item, Stream? stream) {
		ulong? actualSize;
		
		if (stream is not null) {
			actualSize = (ulong) stream.Length;
			
			if (actualSize != item.Size) {
				Log.Warn("Download size differs from its metadata - metadata size: " + item.Size + " B, actual size: " + actualSize + " B, url: " + item.NormalizedUrl);
			}
		}
		else {
			actualSize = item.Size;
		}
		
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
			metadataCmd.Set(":size", actualSize);
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
					ON CONFLICT (normalized_url)
					DO UPDATE SET blob = excluded.blob
					RETURNING rowid
					"""
				);
				
				upsertBlobCmd.AddAndSet(":normalized_url", SqliteType.Text, item.NormalizedUrl);
				upsertBlobCmd.AddAndSet(":blob_length", SqliteType.Integer, actualSize);
				long rowid = await upsertBlobCmd.ExecuteLongScalarAsync();
				
				await using var blob = BlobReference(conn, rowid, readOnly: false);
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
			SkippedWithUnknownSizeCount = reader.GetInt32(11),
		};
	}
	
	public async IAsyncEnumerable<Data.Download> Get(DownloadItemFilter? filter) {
		await using var conn = await pool.Take();
		
		await using var cmd = conn.Command("SELECT normalized_url, download_url, status, type, size FROM download_metadata" + filter.GenerateConditions().BuildWhereClause());
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
	
	public async Task<bool> GetDownloadData(string normalizedUrl, Func<Stream, Task> dataProcessor) {
		await using var conn = await pool.Take();
		
		await using var cmd = conn.Command("SELECT rowid FROM download_blobs WHERE normalized_url = :normalized_url");
		cmd.AddAndSet(":normalized_url", SqliteType.Text, normalizedUrl);
		
		long rowid;
		
		await using (SqliteDataReader reader = await cmd.ExecuteReaderAsync()) {
			if (!await reader.ReadAsync()) {
				return false;
			}
			
			rowid = reader.GetInt64(0);
		}
		
		await using (var blob = BlobReference(conn, rowid, readOnly: true)) {
			await dataProcessor(blob);
		}
		
		return true;
	}
	
	public async Task<bool> GetSuccessfulDownloadWithData(string normalizedUrl, Func<Data.Download, Stream, CancellationToken, Task> dataProcessor, CancellationToken cancellationToken) {
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
		
		await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken)) {
			if (!await reader.ReadAsync(cancellationToken)) {
				return false;
			}
			
			downloadUrl = reader.GetString(0);
			type = reader.IsDBNull(1) ? null : reader.GetString(1);
			rowid = reader.GetInt64(2);
		}
		
		await using (var blob = BlobReference(conn, rowid, readOnly: true)) {
			await dataProcessor(new Data.Download(normalizedUrl, downloadUrl, DownloadStatus.Success, type, (ulong) blob.Length), blob, cancellationToken);
		}
		
		return true;
	}
	
	public async IAsyncEnumerable<DownloadItem> PullPendingDownloadItems(int count, DownloadItemFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken) {
		filter.IncludeStatuses = [DownloadStatus.Pending];
		filter.ExcludeStatuses = null;
		
		var found = new List<DownloadItem>();
		
		await using var conn = await pool.Take();
		
		string sql = $"""
		              SELECT normalized_url, download_url, type, size
		              FROM download_metadata
		              {filter.GenerateConditions().BuildWhereClause()}
		              LIMIT :limit
		              """;
		
		await using (var cmd = conn.Command(sql)) {
			cmd.AddAndSet(":limit", SqliteType.Integer, Math.Max(val1: 0, count));
			
			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			
			while (await reader.ReadAsync(cancellationToken)) {
				var item = new DownloadItem(
					NormalizedUrl: reader.GetString(0),
					DownloadUrl: reader.GetString(1),
					Type: reader.IsDBNull(2) ? null : reader.GetString(2),
					Size: reader.IsDBNull(3) ? null : reader.GetUint64(3)
				);
				
				found.Add(item);
			}
		}
		
		if (found.Count != 0) {
			await using var cmd = conn.Command("UPDATE download_metadata SET status = :downloading WHERE normalized_url = :normalized_url AND status = :pending");
			cmd.AddAndSet(":pending", SqliteType.Integer, (int) DownloadStatus.Pending);
			cmd.AddAndSet(":downloading", SqliteType.Integer, (int) DownloadStatus.Downloading);
			cmd.Add(":normalized_url", SqliteType.Text);
			
			foreach (DownloadItem item in found) {
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
	
	public async Task Remove(ICollection<string> normalizedUrls) {
		await using (var conn = await pool.Take()) {
			await conn.BeginTransactionAsync();
			
			await using (var cmd = conn.Command("DELETE FROM download_metadata WHERE normalized_url = :normalized_url")) {
				cmd.Add(":normalized_url", SqliteType.Text);
				
				foreach (string normalizedUrl in normalizedUrls) {
					cmd.Set(":normalized_url", normalizedUrl);
					await cmd.ExecuteNonQueryAsync();
				}
			}
			
			await conn.CommitTransactionAsync();
		}
		
		UpdateTotalCount();
	}
	
	public async IAsyncEnumerable<FileUrl> FindReachableFiles([EnumeratorCancellation] CancellationToken cancellationToken) {
		await using var conn = await pool.Take();
		
		await using (var cmd = conn.Command("SELECT type, normalized_url, download_url FROM attachments")) {
			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			
			while (await reader.ReadAsync(cancellationToken)) {
				string? type = reader.IsDBNull(0) ? null : reader.GetString(0);
				string normalizedUrl = reader.GetString(1);
				string downloadUrl = reader.GetString(2);
				yield return new FileUrl(normalizedUrl, downloadUrl, type);
			}
		}
		
		await using (var cmd = conn.Command("SELECT json FROM message_embeds")) {
			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			
			while (await reader.ReadAsync(cancellationToken)) {
				if (await DownloadLinkExtractor.TryFromEmbedJson(reader.GetStream(0)) is {} result) {
					yield return result;
				}
			}
		}
		
		await using (var cmd = conn.Command("SELECT DISTINCT emoji_id, emoji_flags FROM message_reactions WHERE emoji_id IS NOT NULL")) {
			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			
			while (await reader.ReadAsync(cancellationToken)) {
				ulong emojiId = reader.GetUint64(0);
				EmojiFlags emojiFlags = (EmojiFlags) reader.GetInt16(1);
				yield return DownloadLinkExtractor.Emoji(emojiId, emojiFlags);
			}
		}
		
		await using (var cmd = conn.Command("SELECT id, type, icon_hash FROM servers WHERE icon_hash IS NOT NULL")) {
			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			
			while (await reader.ReadAsync(cancellationToken)) {
				ulong id = reader.GetUint64(0);
				ServerType? type = ServerTypes.FromString(reader.GetString(1));
				string iconHash = reader.GetString(2);
				
				if (DownloadLinkExtractor.ServerIcon(type, id, iconHash) is {} result) {
					yield return result;
				}
			}
		}
		
		await using (var cmd = conn.Command("SELECT id, avatar_url FROM users WHERE avatar_url IS NOT NULL")) {
			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			
			while (await reader.ReadAsync(cancellationToken)) {
				ulong id = reader.GetUint64(0);
				string avatarHash = reader.GetString(1);
				yield return DownloadLinkExtractor.UserAvatar(id, avatarHash);
			}
		}
	}
}
