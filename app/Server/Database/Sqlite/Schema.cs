using System.Collections.Generic;
using System.Data.Common;
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
		await conn.ExecuteAsync("CREATE TABLE IF NOT EXISTS metadata (key TEXT PRIMARY KEY, value TEXT)");

		var dbVersionStr = await conn.ExecuteReaderAsync("SELECT value FROM metadata WHERE key = 'version'", static reader => reader?.GetString(0));
		if (dbVersionStr == null) {
			await InitializeSchemas();
		}
		else if (!int.TryParse(dbVersionStr, out int dbVersion) || dbVersion < 1) {
			throw new InvalidDatabaseVersionException(dbVersionStr);
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

	private async Task InitializeSchemas() {
		await conn.ExecuteAsync("""
		                        CREATE TABLE users (
		                        	id            INTEGER PRIMARY KEY NOT NULL,
		                        	name          TEXT NOT NULL,
		                        	avatar_url    TEXT,
		                        	discriminator TEXT
		                        )
		                        """);

		await conn.ExecuteAsync("""
		                        CREATE TABLE servers (
		                        	id   INTEGER PRIMARY KEY NOT NULL,
		                        	name TEXT NOT NULL,
		                        	type TEXT NOT NULL
		                        )
		                        """);

		await conn.ExecuteAsync("""
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

		await conn.ExecuteAsync("""
		                        CREATE TABLE messages (
		                        	message_id INTEGER PRIMARY KEY NOT NULL,
		                        	sender_id  INTEGER NOT NULL,
		                        	channel_id INTEGER NOT NULL,
		                        	text       TEXT NOT NULL,
		                        	timestamp  INTEGER NOT NULL
		                        )
		                        """);

		await conn.ExecuteAsync("""
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

		await conn.ExecuteAsync("""
		                        CREATE TABLE embeds (
		                        	message_id INTEGER NOT NULL,
		                        	json       TEXT NOT NULL
		                        )
		                        """);

		await conn.ExecuteAsync("""
		                        CREATE TABLE downloads (
		                        	normalized_url TEXT NOT NULL PRIMARY KEY,
		                        	download_url   TEXT,
		                        	status         INTEGER NOT NULL,
		                        	size           INTEGER NOT NULL,
		                        	blob           BLOB
		                        )
		                        """);

		await conn.ExecuteAsync("""
		                        CREATE TABLE reactions (
		                        	message_id  INTEGER NOT NULL,
		                        	emoji_id    INTEGER,
		                        	emoji_name  TEXT,
		                        	emoji_flags INTEGER NOT NULL,
		                        	count       INTEGER NOT NULL
		                        )
		                        """);

		await CreateMessageEditTimestampTable();
		await CreateMessageRepliedToTable();

		await conn.ExecuteAsync("CREATE INDEX attachments_message_ix ON attachments(message_id)");
		await conn.ExecuteAsync("CREATE INDEX embeds_message_ix ON embeds(message_id)");
		await conn.ExecuteAsync("CREATE INDEX reactions_message_ix ON reactions(message_id)");

		await conn.ExecuteAsync("INSERT INTO metadata (key, value) VALUES ('version', " + Version + ")");
	}

	private async Task CreateMessageEditTimestampTable() {
		await conn.ExecuteAsync("""
		                        CREATE TABLE edit_timestamps (
		                        	message_id     INTEGER PRIMARY KEY NOT NULL,
		                        	edit_timestamp INTEGER NOT NULL
		                        )
		                        """);
	}

	private async Task CreateMessageRepliedToTable() {
		await conn.ExecuteAsync("""
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

		await using var tx = await conn.BeginTransactionAsync();

		int totalUrls = normalizedUrls.Count;
		int processedUrls = -1;

		await using (var updateCmd = conn.Command("UPDATE attachments SET download_url = url, url = :normalized_url WHERE attachment_id = :attachment_id")) {
			updateCmd.Add(":attachment_id", SqliteType.Integer);
			updateCmd.Add(":normalized_url", SqliteType.Text);

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

		await conn.ExecuteAsync("PRAGMA cache_size = -20000");

		DbTransaction tx;

		await using (tx = await conn.BeginTransactionAsync()) {
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

		tx = await conn.BeginTransactionAsync();

		await using (var updateCmd = conn.Command("UPDATE downloads SET download_url = :download_url, url = :normalized_url WHERE url = :download_url")) {
			updateCmd.Add(":normalized_url", SqliteType.Text);
			updateCmd.Add(":download_url", SqliteType.Text);

			foreach (var (normalizedUrl, downloadUrl) in normalizedUrlsToOriginalUrls) {
				if (++processedUrls % 100 == 0) {
					await reporter.SubWork("Updating URLs...", processedUrls, totalUrls);

					// Not proper way of dealing with transactions, but it avoids a long commit at the end.
					// Schema upgrades are already non-atomic anyways, so this doesn't make it worse.
					await tx.CommitAsync();
					await tx.DisposeAsync();

					tx = await conn.BeginTransactionAsync();
					updateCmd.Transaction = (SqliteTransaction) tx;
				}

				updateCmd.Set(":normalized_url", normalizedUrl);
				updateCmd.Set(":download_url", downloadUrl);
				updateCmd.ExecuteNonQuery();
			}
		}

		await reporter.SubWork("Updating URLs...", totalUrls, totalUrls);

		await tx.CommitAsync();
		await tx.DisposeAsync();

		await conn.ExecuteAsync("PRAGMA cache_size = -2000");
	}

	private async Task UpgradeSchemas(int dbVersion, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		var perf = Log.Start("from version " + dbVersion);

		await conn.ExecuteAsync("UPDATE metadata SET value = " + Version + " WHERE key = 'version'");

		if (dbVersion <= 1) {
			await reporter.MainWork("Applying schema changes...", 0, 1);
			await conn.ExecuteAsync("ALTER TABLE channels ADD parent_id INTEGER");

			perf.Step("Upgrade to version 2");
			await reporter.NextVersion();
		}

		if (dbVersion <= 2) {
			await reporter.MainWork("Applying schema changes...", 0, 1);

			await CreateMessageEditTimestampTable();
			await CreateMessageRepliedToTable();

			await conn.ExecuteAsync("""
			                        INSERT INTO edit_timestamps (message_id, edit_timestamp)
			                        SELECT message_id, edit_timestamp
			                        FROM messages
			                        WHERE edit_timestamp IS NOT NULL
			                        """);

			await conn.ExecuteAsync("""
			                        INSERT INTO replied_to (message_id, replied_to_id)
			                        SELECT message_id, replied_to_id
			                        FROM messages
			                        WHERE replied_to_id IS NOT NULL
			                        """);

			await conn.ExecuteAsync("ALTER TABLE messages DROP COLUMN replied_to_id");
			await conn.ExecuteAsync("ALTER TABLE messages DROP COLUMN edit_timestamp");

			perf.Step("Upgrade to version 3");

			await reporter.MainWork("Vacuuming the database...", 1, 1);
			await conn.ExecuteAsync("VACUUM");
			perf.Step("Vacuum");

			await reporter.NextVersion();
		}

		if (dbVersion <= 3) {
			await conn.ExecuteAsync("""
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
			await conn.ExecuteAsync("ALTER TABLE attachments ADD width INTEGER");
			await conn.ExecuteAsync("ALTER TABLE attachments ADD height INTEGER");

			perf.Step("Upgrade to version 5");
			await reporter.NextVersion();
		}

		if (dbVersion <= 5) {
			await reporter.MainWork("Applying schema changes...", 0, 3);
			await conn.ExecuteAsync("ALTER TABLE attachments ADD download_url TEXT");
			await conn.ExecuteAsync("ALTER TABLE downloads ADD download_url TEXT");

			await reporter.MainWork("Updating attachments...", 1, 3);
			await NormalizeAttachmentUrls(reporter);

			await reporter.MainWork("Updating downloads...", 2, 3);
			await NormalizeDownloadUrls(reporter);

			await reporter.MainWork("Applying schema changes...", 3, 3);
			await conn.ExecuteAsync("ALTER TABLE attachments RENAME COLUMN url TO normalized_url");
			await conn.ExecuteAsync("ALTER TABLE downloads RENAME COLUMN url TO normalized_url");

			perf.Step("Upgrade to version 6");
			await reporter.NextVersion();
		}

		perf.End();
	}
}
