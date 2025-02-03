using System.Collections.Generic;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Server.Download;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Schema;

sealed class SqliteSchemaUpgradeTo7 : ISchemaUpgrade {
	async Task ISchemaUpgrade.Run(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.MainWork("Applying schema changes...", finishedItems: 0, totalItems: 6);
		await SqliteSchema.CreateDownloadTables(conn);
		
		await reporter.MainWork("Migrating download metadata...", finishedItems: 1, totalItems: 6);
		await conn.ExecuteAsync("INSERT INTO download_metadata (normalized_url, download_url, status, size) SELECT normalized_url, download_url, status, size FROM downloads");
		
		await reporter.MainWork("Merging attachment metadata...", finishedItems: 2, totalItems: 6);
		await conn.ExecuteAsync("UPDATE download_metadata SET type = (SELECT type FROM attachments WHERE download_metadata.normalized_url = attachments.normalized_url)");
		
		await reporter.MainWork("Migrating downloaded files...", finishedItems: 3, totalItems: 6);
		await MigrateDownloadBlobsToNewTable(conn, reporter);
		
		await reporter.MainWork("Applying schema changes...", finishedItems: 4, totalItems: 6);
		await conn.ExecuteAsync("DROP TABLE downloads");
		
		await reporter.MainWork("Discovering downloadable links...", finishedItems: 5, totalItems: 6);
		await DiscoverDownloadableLinks(conn, reporter);
	}
	
	private async Task MigrateDownloadBlobsToNewTable(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.SubWork("Listing downloaded files...", finishedItems: 0, totalItems: 0);
		
		List<string> urlsToMigrate = await GetDownloadedFileUrls(conn);
		int totalFiles = urlsToMigrate.Count;
		int processedFiles = -1;
		
		await reporter.SubWork("Processing downloaded files...", finishedItems: 0, totalFiles);
		
		await conn.BeginTransactionAsync();
		
		await using (var insertCmd = conn.Command("INSERT INTO download_blobs (normalized_url, blob) SELECT normalized_url, blob FROM downloads WHERE normalized_url = :normalized_url"))
		await using (var deleteCmd = conn.Command("DELETE FROM downloads WHERE normalized_url = :normalized_url")) {
			insertCmd.Add(":normalized_url", SqliteType.Text);
			deleteCmd.Add(":normalized_url", SqliteType.Text);
			
			foreach (string url in urlsToMigrate) {
				if (++processedFiles % 10 == 0) {
					await reporter.SubWork("Processing downloaded files...", processedFiles, totalFiles);
					
					// Not proper way of dealing with transactions, but it avoids a long commit at the end.
					// Schema upgrades are already non-atomic anyways, so this doesn't make it worse.
					await conn.CommitTransactionAsync();
					
					await conn.BeginTransactionAsync();
					conn.AssignActiveTransaction(insertCmd);
					conn.AssignActiveTransaction(deleteCmd);
				}
				
				insertCmd.Set(":normalized_url", url);
				await insertCmd.ExecuteNonQueryAsync();
				
				deleteCmd.Set(":normalized_url", url);
				await deleteCmd.ExecuteNonQueryAsync();
			}
		}
		
		await reporter.SubWork("Processing downloaded files...", totalFiles, totalFiles);
		
		await conn.CommitTransactionAsync();
	}
	
	private async Task<List<string>> GetDownloadedFileUrls(ISqliteConnection conn) {
		var urls = new List<string>();
		
		await using var selectCmd = conn.Command("SELECT normalized_url FROM downloads WHERE blob IS NOT NULL");
		await using var reader = await selectCmd.ExecuteReaderAsync();
		
		while (await reader.ReadAsync()) {
			urls.Add(reader.GetString(0));
		}
		
		return urls;
	}
	
	private async Task DiscoverDownloadableLinks(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.SubWork("Processing attachments...", finishedItems: 0, totalItems: 4);
		
		await using (var cmd = conn.Command("""
		                                    INSERT OR IGNORE INTO download_metadata (normalized_url, download_url, status, type, size)
		                                    SELECT a.normalized_url, a.download_url, :pending, a.type, MAX(a.size)
		                                    FROM attachments a
		                                    GROUP BY a.normalized_url
		                                    """)) {
			cmd.AddAndSet(":pending", SqliteType.Integer, (int) DownloadStatus.Pending);
			await cmd.ExecuteNonQueryAsync();
		}
		
		static async Task InsertDownload(SqliteCommand insertCmd, Data.Download? download) {
			if (download == null) {
				return;
			}
			
			insertCmd.Set(":normalized_url", download.NormalizedUrl);
			insertCmd.Set(":download_url", download.DownloadUrl);
			insertCmd.Set(":status", (int) download.Status);
			insertCmd.Set(":type", download.Type);
			insertCmd.Set(":size", download.Size);
			await insertCmd.ExecuteNonQueryAsync();
		}
		
		await conn.BeginTransactionAsync();
		
		await using var insertCmd = conn.Command("INSERT OR IGNORE INTO download_metadata (normalized_url, download_url, status, type, size) VALUES (:normalized_url, :download_url, :status, :type, :size)");
		insertCmd.Add(":normalized_url", SqliteType.Text);
		insertCmd.Add(":download_url", SqliteType.Text);
		insertCmd.Add(":status", SqliteType.Integer);
		insertCmd.Add(":type", SqliteType.Text);
		insertCmd.Add(":size", SqliteType.Integer);
		
		await reporter.SubWork("Processing embeds...", finishedItems: 1, totalItems: 4);
		
		await using (var embedCmd = conn.Command("SELECT json FROM embeds")) {
			await using var reader = await embedCmd.ExecuteReaderAsync();
			
			while (await reader.ReadAsync()) {
				await InsertDownload(insertCmd, await DownloadLinkExtractor.TryFromEmbedJson(reader.GetStream(0)));
			}
		}
		
		await reporter.SubWork("Processing users...", finishedItems: 2, totalItems: 4);
		
		await using (var avatarCmd = conn.Command("SELECT id, avatar_url FROM users WHERE avatar_url IS NOT NULL")) {
			await using var reader = await avatarCmd.ExecuteReaderAsync();
			
			while (await reader.ReadAsync()) {
				await InsertDownload(insertCmd, DownloadLinkExtractor.FromUserAvatar(reader.GetUint64(0), reader.GetString(1)));
			}
		}
		
		await reporter.SubWork("Processing reactions...", finishedItems: 3, totalItems: 4);
		
		await using (var avatarCmd = conn.Command("SELECT DISTINCT emoji_id, emoji_flags FROM reactions WHERE emoji_id IS NOT NULL")) {
			await using var reader = await avatarCmd.ExecuteReaderAsync();
			
			while (await reader.ReadAsync()) {
				await InsertDownload(insertCmd, DownloadLinkExtractor.FromEmoji(reader.GetUint64(0), (EmojiFlags) reader.GetInt16(1)));
			}
		}
		
		await conn.CommitTransactionAsync();
	}
}
