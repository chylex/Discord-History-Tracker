using System.Collections.Generic;
using System.Threading.Tasks;
using DHT.Server.Database.Exceptions;
using DHT.Server.Database.Sqlite.Schema;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Utils.Logging;

namespace DHT.Server.Database.Sqlite;

sealed class SqliteSchema {
	internal const int Version = 9;

	private static readonly Log Log = Log.ForType<SqliteSchema>();

	private readonly ISqliteConnection conn;

	public SqliteSchema(ISqliteConnection conn) {
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
		                        	display_name  TEXT,
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
		                        CREATE TABLE message_embeds (
		                        	message_id INTEGER NOT NULL,
		                        	json       TEXT NOT NULL
		                        )
		                        """);

		await conn.ExecuteAsync("""
		                        CREATE TABLE message_reactions (
		                        	message_id  INTEGER NOT NULL,
		                        	emoji_id    INTEGER,
		                        	emoji_name  TEXT,
		                        	emoji_flags INTEGER NOT NULL,
		                        	count       INTEGER NOT NULL
		                        )
		                        """);

		await CreateMessageEditTimestampTable(conn);
		await CreateMessageRepliedToTable(conn);
		await CreateDownloadTables(conn);
		await CreateMessageAttachmentsTable(conn);

		await conn.ExecuteAsync("CREATE INDEX embeds_message_ix ON message_embeds(message_id)");
		await conn.ExecuteAsync("CREATE INDEX reactions_message_ix ON message_reactions(message_id)");

		await conn.ExecuteAsync("INSERT INTO metadata (key, value) VALUES ('version', " + Version + ")");
	}

	internal static async Task CreateMessageEditTimestampTable(ISqliteConnection conn) {
		await conn.ExecuteAsync("""
		                        CREATE TABLE message_edit_timestamps (
		                        	message_id     INTEGER PRIMARY KEY NOT NULL,
		                        	edit_timestamp INTEGER NOT NULL
		                        )
		                        """);
	}

	internal static async Task CreateMessageRepliedToTable(ISqliteConnection conn) {
		await conn.ExecuteAsync("""
		                        CREATE TABLE message_replied_to (
		                        	message_id    INTEGER PRIMARY KEY NOT NULL,
		                        	replied_to_id INTEGER NOT NULL
		                        )
		                        """);
	}

	internal static async Task CreateDownloadTables(ISqliteConnection conn) {
		await conn.ExecuteAsync("""
		                        CREATE TABLE download_metadata (
		                        	normalized_url TEXT NOT NULL PRIMARY KEY,
		                        	download_url   TEXT NOT NULL,
		                        	status         INTEGER NOT NULL,
		                        	type           TEXT,
		                        	size           INTEGER
		                        )
		                        """);

		await conn.ExecuteAsync("""
		                        CREATE TABLE download_blobs (
		                        	normalized_url TEXT NOT NULL PRIMARY KEY,
		                        	blob           BLOB NOT NULL,
		                        	FOREIGN KEY (normalized_url) REFERENCES download_metadata (normalized_url) ON UPDATE CASCADE ON DELETE CASCADE
		                        )
		                        """);
	}
	
	internal static async Task CreateMessageAttachmentsTable(ISqliteConnection conn) {
		await conn.ExecuteAsync("""
		                        CREATE TABLE message_attachments (
		                        	message_id    INTEGER NOT NULL,
		                        	attachment_id INTEGER NOT NULL,
		                        	PRIMARY KEY (message_id, attachment_id),
		                            FOREIGN KEY (message_id) REFERENCES messages (message_id) ON UPDATE CASCADE ON DELETE CASCADE,
		                            FOREIGN KEY (attachment_id) REFERENCES attachments (attachment_id) ON UPDATE CASCADE ON DELETE CASCADE
		                        )
		                        """);
	}

	private async Task UpgradeSchemas(int dbVersion, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		var upgrades = new Dictionary<int, ISchemaUpgrade> {
			{ 1, new SqliteSchemaUpgradeTo2() },
			{ 2, new SqliteSchemaUpgradeTo3() },
			{ 3, new SqliteSchemaUpgradeTo4() },
			{ 4, new SqliteSchemaUpgradeTo5() },
			{ 5, new SqliteSchemaUpgradeTo6() },
			{ 6, new SqliteSchemaUpgradeTo7() },
			{ 7, new SqliteSchemaUpgradeTo8() },
			{ 8, new SqliteSchemaUpgradeTo9() },
		};

		var perf = Log.Start("from version " + dbVersion);

		for (int fromVersion = dbVersion; fromVersion < Version; fromVersion++) {
			var toVersion = fromVersion + 1;

			if (upgrades.TryGetValue(fromVersion, out var upgrade)) {
				await upgrade.Run(conn, reporter);
			}

			await conn.ExecuteAsync("UPDATE metadata SET value = " + toVersion + " WHERE key = 'version'");

			perf.Step("Upgrade to version " + toVersion);
			await reporter.NextVersion();
		}

		perf.End();
	}
}
