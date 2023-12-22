using System.Collections.Generic;
using System.Threading.Tasks;
using DHT.Server.Database.Exceptions;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Server.Download;
using DHT.Utils.Logging;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite;

sealed class Schema {
	internal const int Version = 6;

	private static readonly Log Log = Log.ForType<Schema>();

	private readonly ISqliteConnection conn;

	public Schema(ISqliteConnection conn) {
		this.conn = conn;
	}

	public async Task<bool> Setup(ISchemaUpgradeCallbacks callbacks) {
		conn.Execute(@"CREATE TABLE IF NOT EXISTS metadata (key TEXT PRIMARY KEY, value TEXT)");

		var dbVersionStr = conn.SelectScalar("SELECT value FROM metadata WHERE key = 'version'");
		if (dbVersionStr == null) {
			InitializeSchemas();
		}
		else if (!int.TryParse(dbVersionStr.ToString(), out int dbVersion) || dbVersion < 1) {
			throw new InvalidDatabaseVersionException(dbVersionStr.ToString() ?? "<null>");
		}
		else if (dbVersion > Version) {
			throw new DatabaseTooNewException(dbVersion);
		}
		else if (dbVersion < Version) {
			var proceed = await callbacks.CanUpgrade();
			if (!proceed) {
				return false;
			}

			await callbacks.Start(Version - dbVersion, async reporter => await UpgradeSchemas(dbVersion, reporter));
		}

		return true;
	}

	private void InitializeSchemas() {
		conn.Execute("""
		             CREATE TABLE users (
		             	id            INTEGER PRIMARY KEY NOT NULL,
		             	name          TEXT NOT NULL,
		             	avatar_url    TEXT,
		             	discriminator TEXT
		             )
		             """);

		conn.Execute("""
		             CREATE TABLE servers (
		             	id   INTEGER PRIMARY KEY NOT NULL,
		             	name TEXT NOT NULL,
		             	type TEXT NOT NULL
		             )
		             """);

		conn.Execute("""
		             CREATE TABLE channels (
		             	id        INTEGER PRIMARY KEY NOT NULL,
		             	server    INTEGER NOT NULL,
		             	name      TEXT NOT NULL,
		             	parent_id INTEGER,
		             	position  INTEGER,
		             	topic     TEXT,
		             	nsfw      INTEGER
		             )
		             """);

		conn.Execute("""
		             CREATE TABLE messages (
		             	message_id INTEGER PRIMARY KEY NOT NULL,
		             	sender_id  INTEGER NOT NULL,
		             	channel_id INTEGER NOT NULL,
		             	text       TEXT NOT NULL,
		             	timestamp  INTEGER NOT NULL
		             )
		             """);

		conn.Execute("""
		             CREATE TABLE attachments (
		             	message_id     INTEGER NOT NULL,
		             	attachment_id  INTEGER NOT NULL PRIMARY KEY NOT NULL,
		             	name           TEXT NOT NULL,
		             	type           TEXT,
		             	normalized_url TEXT NOT NULL,
		             	download_url   TEXT,
		             	size           INTEGER NOT NULL,
		             	width          INTEGER,
		             	height         INTEGER
		             )
		             """);

		conn.Execute("""
		             CREATE TABLE embeds (
		             	message_id INTEGER NOT NULL,
		             	json       TEXT NOT NULL
		             )
		             """);

		conn.Execute("""
		             CREATE TABLE downloads (
		             	normalized_url TEXT NOT NULL PRIMARY KEY,
		             	download_url   TEXT,
		             	status         INTEGER NOT NULL,
		             	size           INTEGER NOT NULL,
		             	blob           BLOB
		             )
		             """);
		
		conn.Execute("""
		             CREATE TABLE reactions (
		             	message_id  INTEGER NOT NULL,
		             	emoji_id    INTEGER,
		             	emoji_name  TEXT,
		             	emoji_flags INTEGER NOT NULL,
		             	count       INTEGER NOT NULL
		             )
		             """);

		CreateMessageEditTimestampTable();
		CreateMessageRepliedToTable();

		conn.Execute("CREATE INDEX attachments_message_ix ON attachments(message_id)");
		conn.Execute("CREATE INDEX embeds_message_ix ON embeds(message_id)");
		conn.Execute("CREATE INDEX reactions_message_ix ON reactions(message_id)");

		conn.Execute("INSERT INTO metadata (key, value) VALUES ('version', " + Version + ")");
	}

	private void CreateMessageEditTimestampTable() {
		conn.Execute("""
		             CREATE TABLE edit_timestamps (
		             	message_id     INTEGER PRIMARY KEY NOT NULL,
		             	edit_timestamp INTEGER NOT NULL
		             )
		             """);
	}

	private void CreateMessageRepliedToTable() {
		conn.Execute("""
		             CREATE TABLE replied_to (
		             	message_id    INTEGER PRIMARY KEY NOT NULL,
		             	replied_to_id INTEGER NOT NULL
		             )
		             """);
	}

	private async Task NormalizeAttachmentUrls(ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.SubWork("Preparing attachments...", 0, 0);
		
		var normalizedUrls = new Dictionary<long, string>();

		await using (var selectCmd = conn.Command("SELECT attachment_id, url FROM attachments")) {
			await using var reader = await selectCmd.ExecuteReaderAsync();
			
			while (reader.Read()) {
				var attachmentId = reader.GetInt64(0);
				var originalUrl = reader.GetString(1);
				normalizedUrls[attachmentId] = DiscordCdn.NormalizeUrl(originalUrl);
			}
		}

		await using var tx = conn.BeginTransaction();

		int totalUrls = normalizedUrls.Count;
		int processedUrls = -1;

		await using (var updateCmd = conn.Command("UPDATE attachments SET download_url = url, url = :normalized_url WHERE attachment_id = :attachment_id")) {
			updateCmd.Parameters.Add(":attachment_id", SqliteType.Integer);
			updateCmd.Parameters.Add(":normalized_url", SqliteType.Text);
				
			foreach (var (attachmentId, normalizedUrl) in normalizedUrls) {
				if (++processedUrls % 1000 == 0) {
					await reporter.SubWork("Updating URLs...", processedUrls, totalUrls);
				}

				updateCmd.Set(":attachment_id", attachmentId);
				updateCmd.Set(":normalized_url", normalizedUrl);
				updateCmd.ExecuteNonQuery();
			}
		}
		
		await reporter.SubWork("Updating URLs...", totalUrls, totalUrls);
		
		await tx.CommitAsync();
	}

	private async Task NormalizeDownloadUrls(ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.SubWork("Preparing downloads...", 0, 0);
		
		var normalizedUrlsToOriginalUrls = new Dictionary<string, string>();
		var duplicateUrlsToDelete = new HashSet<string>();

		await using (var selectCmd = conn.Command("SELECT url FROM downloads ORDER BY CASE WHEN status = 200 THEN 0 ELSE 1 END")) {
			await using var reader = await selectCmd.ExecuteReaderAsync();

			while (reader.Read()) {
				var originalUrl = reader.GetString(0);
				var normalizedUrl = DiscordCdn.NormalizeUrl(originalUrl);

				if (!normalizedUrlsToOriginalUrls.TryAdd(normalizedUrl, originalUrl)) {
					duplicateUrlsToDelete.Add(originalUrl);
				}
			}
		}

		conn.Execute("PRAGMA cache_size = -20000");

		SqliteTransaction tx;
		
		await using (tx = conn.BeginTransaction()) {
			await reporter.SubWork("Deleting duplicates...", 0, 0);

			await using (var deleteCmd = conn.Delete("downloads", ("url", SqliteType.Text))) {
				foreach (var duplicateUrl in duplicateUrlsToDelete) {
					deleteCmd.Set(":url", duplicateUrl);
					deleteCmd.ExecuteNonQuery();
				}
			}
			
			await tx.CommitAsync();
		}

		int totalUrls = normalizedUrlsToOriginalUrls.Count;
		int processedUrls = -1;

		tx = conn.BeginTransaction();
		
		await using (var updateCmd = conn.Command("UPDATE downloads SET download_url = :download_url, url = :normalized_url WHERE url = :download_url")) {
			updateCmd.Parameters.Add(":normalized_url", SqliteType.Text);
			updateCmd.Parameters.Add(":download_url", SqliteType.Text);
			
			foreach (var (normalizedUrl, downloadUrl) in normalizedUrlsToOriginalUrls) {
				if (++processedUrls % 100 == 0) {
					await reporter.SubWork("Updating URLs...", processedUrls, totalUrls);
					
					// Not proper way of dealing with transactions, but it avoids a long commit at the end.
					// Schema upgrades are already non-atomic anyways, so this doesn't make it worse.
					await tx.CommitAsync();
					await tx.DisposeAsync();
					
					tx = conn.BeginTransaction();
					updateCmd.Transaction = tx;
				}

				updateCmd.Set(":normalized_url", normalizedUrl);
				updateCmd.Set(":download_url", downloadUrl);
				updateCmd.ExecuteNonQuery();
			}
		}
		
		await reporter.SubWork("Updating URLs...", totalUrls, totalUrls);
		
		await tx.CommitAsync();
		await tx.DisposeAsync();
		
		conn.Execute("PRAGMA cache_size = -2000");
	}

	private async Task UpgradeSchemas(int dbVersion, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		var perf = Log.Start("from version " + dbVersion);

		conn.Execute("UPDATE metadata SET value = " + Version + " WHERE key = 'version'");

		if (dbVersion <= 1) {
			await reporter.MainWork("Applying schema changes...", 0, 1);
			conn.Execute("ALTER TABLE channels ADD parent_id INTEGER");
			
			perf.Step("Upgrade to version 2");
			await reporter.NextVersion();
		}

		if (dbVersion <= 2) {
			await reporter.MainWork("Applying schema changes...", 0, 1);
			
			CreateMessageEditTimestampTable();
			CreateMessageRepliedToTable();

			conn.Execute("""
			             INSERT INTO edit_timestamps (message_id, edit_timestamp)
			             SELECT message_id, edit_timestamp
			             FROM messages
			             WHERE edit_timestamp IS NOT NULL
			             """);

			conn.Execute("""
			             INSERT INTO replied_to (message_id, replied_to_id)
			             SELECT message_id, replied_to_id
			             FROM messages
			             WHERE replied_to_id IS NOT NULL
			             """);

			conn.Execute("ALTER TABLE messages DROP COLUMN replied_to_id");
			conn.Execute("ALTER TABLE messages DROP COLUMN edit_timestamp");

			perf.Step("Upgrade to version 3");
			
			await reporter.MainWork("Vacuuming the database...", 1, 1);
			conn.Execute("VACUUM");
			perf.Step("Vacuum");
			
			await reporter.NextVersion();
		}

		if (dbVersion <= 3) {
			conn.Execute("""
			             CREATE TABLE downloads (
			             	url    TEXT NOT NULL PRIMARY KEY,
			             	status INTEGER NOT NULL,
			             	size   INTEGER NOT NULL,
			             	blob   BLOB
			             )
			             """);
			
			perf.Step("Upgrade to version 4");
			await reporter.NextVersion();
		}

		if (dbVersion <= 4) {
			await reporter.MainWork("Applying schema changes...", 0, 1);
			conn.Execute("ALTER TABLE attachments ADD width INTEGER");
			conn.Execute("ALTER TABLE attachments ADD height INTEGER");
			
			perf.Step("Upgrade to version 5");
			await reporter.NextVersion();
		}

		if (dbVersion <= 5) {
			await reporter.MainWork("Applying schema changes...", 0, 3);
			conn.Execute("ALTER TABLE attachments ADD download_url TEXT");
			conn.Execute("ALTER TABLE downloads ADD download_url TEXT");
			
			await reporter.MainWork("Updating attachments...", 1, 3);
			await NormalizeAttachmentUrls(reporter);
			
			await reporter.MainWork("Updating downloads...", 2, 3);
			await NormalizeDownloadUrls(reporter);
			
			await reporter.MainWork("Applying schema changes...", 3, 3);
			conn.Execute("ALTER TABLE attachments RENAME COLUMN url TO normalized_url");
			conn.Execute("ALTER TABLE downloads RENAME COLUMN url TO normalized_url");
			
			perf.Step("Upgrade to version 6");
			await reporter.NextVersion();
		}

		perf.End();
	}
}
