using System;
using System.Threading.Tasks;
using DHT.Server.Database.Exceptions;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Utils.Logging;

namespace DHT.Server.Database.Sqlite;

sealed class Schema {
	internal const int Version = 1;

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
		CREATE TABLE guilds (
			guild_id INTEGER NOT NULL,
			json TEXT NOT NULL,
			PRIMARY KEY (guild_id)
		)
		""");
		
		Execute("""
		CREATE TABLE channels (
			channel_id INTEGER NOT NULL,
			guild_id INTEGER NOT NULL,
			json TEXT NOT NULL,
			PRIMARY KEY (channel_id),
			FOREIGN KEY (guild_id) REFERENCES guilds(guild_id)
		)
		""");
		
		Execute("""
		CREATE TABLE messages (
		    message_id INTEGER NOT NULL,
		    channel_id INTEGER NOT NULL,
		    timestamp TEXT NOT NULL,
		    json TEXT NOT NULL,
		    PRIMARY KEY (message_id, timestamp),
		    FOREIGN KEY (channel_id) REFERENCES channels(channel_id)
		)
		""");
		
		Execute("INSERT INTO metadata (key, value) VALUES ('version', " + Version + ")");
	}
	
	private void UpgradeSchemas(int dbVersion) {
		var perf = Log.Start("from version " + dbVersion);

		Execute("UPDATE metadata SET value = " + Version + " WHERE key = 'version'");

		// if (dbVersion <= 1) {
		// 	perf.Step("Upgrade to version 2");
		// }

		perf.End();
	}
}
