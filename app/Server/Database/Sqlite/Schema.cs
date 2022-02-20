using System;
using System.Threading.Tasks;
using DHT.Server.Database.Exceptions;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite {
	sealed class Schema {
		internal const int Version = 2;

		private readonly SqliteConnection conn;

		public Schema(SqliteConnection conn) {
			this.conn = conn;
		}

		private SqliteCommand Sql(string sql) {
			var cmd = conn.CreateCommand();
			cmd.CommandText = sql;
			return cmd;
		}

		private void Execute(string sql) {
			Sql(sql).ExecuteNonQuery();
		}

		public async Task<bool> Setup(Func<Task<bool>> checkCanUpgradeSchemas) {
			Execute(@"CREATE TABLE IF NOT EXISTS metadata (key TEXT PRIMARY KEY, value TEXT)");

			var dbVersionStr = Sql("SELECT value FROM metadata WHERE key = 'version'").ExecuteScalar();
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
			Execute(@"CREATE TABLE users (
			          id INTEGER PRIMARY KEY NOT NULL,
			          name TEXT NOT NULL,
			          avatar_url TEXT,
			          discriminator TEXT)");

			Execute(@"CREATE TABLE servers (
			          id INTEGER PRIMARY KEY NOT NULL,
			          name TEXT NOT NULL,
			          type TEXT NOT NULL)");

			Execute(@"CREATE TABLE channels (
			          id INTEGER PRIMARY KEY NOT NULL,
			          server INTEGER NOT NULL,
			          name TEXT NOT NULL,
			          parent_id INTEGER,
			          position INTEGER,
			          topic TEXT,
			          nsfw INTEGER)");

			Execute(@"CREATE TABLE messages (
			        message_id INTEGER PRIMARY KEY NOT NULL,
			        sender_id INTEGER NOT NULL,
			        channel_id INTEGER NOT NULL,
			        text TEXT NOT NULL,
			        timestamp INTEGER NOT NULL,
			        edit_timestamp INTEGER,
			        replied_to_id INTEGER)");

			Execute(@"CREATE TABLE attachments (
			        message_id INTEGER NOT NULL,
			        attachment_id INTEGER NOT NULL PRIMARY KEY NOT NULL,
			        name TEXT NOT NULL,
			        type TEXT,
			        url TEXT NOT NULL,
			        size INTEGER NOT NULL)");

			Execute(@"CREATE TABLE embeds (
			        message_id INTEGER NOT NULL,
			        json TEXT NOT NULL)");

			Execute(@"CREATE TABLE reactions (
					message_id INTEGER NOT NULL,
					emoji_id INTEGER,
					emoji_name TEXT,
					emoji_flags INTEGER NOT NULL,
					count INTEGER NOT NULL)");

			Execute("CREATE INDEX attachments_message_ix ON attachments(message_id)");
			Execute("CREATE INDEX embeds_message_ix ON embeds(message_id)");
			Execute("CREATE INDEX reactions_message_ix ON reactions(message_id)");

			Execute("INSERT INTO metadata (key, value) VALUES ('version', " + Version + ")");
		}

		private void UpgradeSchemas(int dbVersion) {
			Execute("UPDATE metadata SET value = " + Version + " WHERE key = 'version'");

			if (dbVersion <= 1) {
				Execute("ALTER TABLE channels ADD parent_id INTEGER");
			}
		}
	}
}
