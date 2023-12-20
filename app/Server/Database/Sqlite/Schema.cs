using System;
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

	private void Execute(string sql) {
		conn.Command(sql).ExecuteNonQuery();
	}

	public async Task<bool> Setup(Func<Task<bool>> checkCanUpgradeSchemas) {
		Execute(@"CREATE TABLE IF NOT EXISTS metadata (key TEXT PRIMARY KEY, value TEXT)");

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
			var proceed = await checkCanUpgradeSchemas();
			if (!proceed) {
				return false;
			}

			UpgradeSchemas(dbVersion);
		}

		return true;
	}

	private void InitializeSchemas() {
		Execute("""
				CREATE TABLE users (
					id            INTEGER PRIMARY KEY NOT NULL,
					name          TEXT NOT NULL,
					avatar_url    TEXT,
					discriminator TEXT
				)
				""");

		Execute("""
				CREATE TABLE servers (
					id   INTEGER PRIMARY KEY NOT NULL,
					name TEXT NOT NULL,
					type TEXT NOT NULL
				)
				""");

		Execute("""
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

		Execute("""
				CREATE TABLE messages (
					message_id INTEGER PRIMARY KEY NOT NULL,
					sender_id  INTEGER NOT NULL,
					channel_id INTEGER NOT NULL,
					text       TEXT NOT NULL,
					timestamp  INTEGER NOT NULL
				)
				""");

		Execute("""
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

		Execute("""
				CREATE TABLE embeds (
					message_id INTEGER NOT NULL,
					json       TEXT NOT NULL
				)
				""");

		Execute("""
				CREATE TABLE downloads (
					normalized_url TEXT NOT NULL PRIMARY KEY,
					download_url   TEXT,
					status         INTEGER NOT NULL,
					size           INTEGER NOT NULL,
					blob           BLOB
				)
				""");
		
		Execute("""
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

		Execute("CREATE INDEX attachments_message_ix ON attachments(message_id)");
		Execute("CREATE INDEX embeds_message_ix ON embeds(message_id)");
		Execute("CREATE INDEX reactions_message_ix ON reactions(message_id)");

		Execute("INSERT INTO metadata (key, value) VALUES ('version', " + Version + ")");
	}

	private void CreateMessageEditTimestampTable() {
		Execute("""
				CREATE TABLE edit_timestamps (
					message_id     INTEGER PRIMARY KEY NOT NULL,
					edit_timestamp INTEGER NOT NULL
				)
				""");
	}

	private void CreateMessageRepliedToTable() {
		Execute("""
				CREATE TABLE replied_to (
					message_id    INTEGER PRIMARY KEY NOT NULL,
					replied_to_id INTEGER NOT NULL
				)
				""");
	}

	private void NormalizeAttachmentUrls() {
		var normalizedUrls = new Dictionary<long, string>();

		using (var selectCmd = conn.Command("SELECT attachment_id, url FROM attachments")) {
			using var reader = selectCmd.ExecuteReader();
			
			while (reader.Read()) {
				var attachmentId = reader.GetInt64(0);
				var originalUrl = reader.GetString(1);
				normalizedUrls[attachmentId] = DiscordCdn.NormalizeUrl(originalUrl);
			}
		}

		using var tx = conn.BeginTransaction();
		
		using (var updateCmd = conn.Command("UPDATE attachments SET download_url = url, url = :normalized_url WHERE attachment_id = :attachment_id")) {
			updateCmd.Parameters.Add(":attachment_id", SqliteType.Integer);
			updateCmd.Parameters.Add(":normalized_url", SqliteType.Text);
				
			foreach (var (attachmentId, normalizedUrl) in normalizedUrls) {
				updateCmd.Set(":attachment_id", attachmentId);
				updateCmd.Set(":normalized_url", normalizedUrl);
				updateCmd.ExecuteNonQuery();
			}
		}
			
		tx.Commit();
	}

	private void NormalizeDownloadUrls() {
		var normalizedUrlsToOriginalUrls = new Dictionary<string, string>();
		var duplicateUrlsToDelete = new HashSet<string>();

		using (var selectCmd = conn.Command("SELECT url FROM downloads ORDER BY CASE WHEN status = 200 THEN 0 ELSE 1 END")) {
			using var reader = selectCmd.ExecuteReader();

			while (reader.Read()) {
				var originalUrl = reader.GetString(0);
				var normalizedUrl = DiscordCdn.NormalizeUrl(originalUrl);

				if (!normalizedUrlsToOriginalUrls.TryAdd(normalizedUrl, originalUrl)) {
					duplicateUrlsToDelete.Add(originalUrl);
				}
			}
		}

		using var tx = conn.BeginTransaction();
		
		using (var deleteCmd = conn.Delete("downloads", ("url", SqliteType.Text))) {
			foreach (var duplicateUrl in duplicateUrlsToDelete) {
				deleteCmd.Set(":url", duplicateUrl);
				deleteCmd.ExecuteNonQuery();
			}
		}
			
		using (var updateCmd = conn.Command("UPDATE downloads SET download_url = :download_url, url = :normalized_url WHERE url = :download_url")) {
			updateCmd.Parameters.Add(":normalized_url", SqliteType.Text);
			updateCmd.Parameters.Add(":download_url", SqliteType.Text);
				
			foreach (var (normalizedUrl, downloadUrl) in normalizedUrlsToOriginalUrls) {
				updateCmd.Set(":normalized_url", normalizedUrl);
				updateCmd.Set(":download_url", downloadUrl);
				updateCmd.ExecuteNonQuery();
			}
		}
			
		tx.Commit();
	}

	private void UpgradeSchemas(int dbVersion) {
		var perf = Log.Start("from version " + dbVersion);

		Execute("UPDATE metadata SET value = " + Version + " WHERE key = 'version'");

		if (dbVersion <= 1) {
			Execute("ALTER TABLE channels ADD parent_id INTEGER");
			perf.Step("Upgrade to version 2");
		}

		if (dbVersion <= 2) {
			CreateMessageEditTimestampTable();
			CreateMessageRepliedToTable();

			Execute("""
					INSERT INTO edit_timestamps (message_id, edit_timestamp)
					SELECT message_id, edit_timestamp
					FROM messages
					WHERE edit_timestamp IS NOT NULL
					""");

			Execute("""
					INSERT INTO replied_to (message_id, replied_to_id)
					SELECT message_id, replied_to_id
					FROM messages
					WHERE replied_to_id IS NOT NULL
					""");

			Execute("ALTER TABLE messages DROP COLUMN replied_to_id");
			Execute("ALTER TABLE messages DROP COLUMN edit_timestamp");

			perf.Step("Upgrade to version 3");

			Execute("VACUUM");
			perf.Step("Vacuum");
		}

		if (dbVersion <= 3) {
			Execute("""
					CREATE TABLE downloads (
						url    TEXT NOT NULL PRIMARY KEY,
						status INTEGER NOT NULL,
						size   INTEGER NOT NULL,
						blob   BLOB
					)
					""");
			
			perf.Step("Upgrade to version 4");
		}

		if (dbVersion <= 4) {
			Execute("ALTER TABLE attachments ADD width INTEGER");
			Execute("ALTER TABLE attachments ADD height INTEGER");
			perf.Step("Upgrade to version 5");
		}

		if (dbVersion <= 5) {
			Execute("ALTER TABLE attachments ADD download_url TEXT");
			Execute("ALTER TABLE downloads ADD download_url TEXT");
			
			NormalizeAttachmentUrls();
			NormalizeDownloadUrls();
			
			Execute("ALTER TABLE attachments RENAME COLUMN url TO normalized_url");
			Execute("ALTER TABLE downloads RENAME COLUMN url TO normalized_url");
			
			perf.Step("Upgrade to version 6");
		}

		perf.End();
	}
}
