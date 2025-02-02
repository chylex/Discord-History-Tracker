using System.Threading.Tasks;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Utils.Logging;

namespace DHT.Server.Database.Sqlite.Schema;

sealed class SqliteSchemaUpgradeTo10 : ISchemaUpgrade {
	private static readonly Log Log = Log.ForType<SqliteSchemaUpgradeTo10>();
	
	async Task ISchemaUpgrade.Run(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.MainWork("Migrating message embeds...", finishedItems: 0, totalItems: 6);
		await conn.ExecuteAsync("""
		                        CREATE TABLE message_embeds_new (
		                        	message_id INTEGER NOT NULL,
		                        	json       TEXT NOT NULL,
		                        	FOREIGN KEY (message_id) REFERENCES messages (message_id) ON UPDATE CASCADE ON DELETE CASCADE
		                        )
		                        """);
		await conn.ExecuteAsync("INSERT INTO message_embeds_new (message_id, json) SELECT message_id, json FROM message_embeds WHERE message_id IN (SELECT DISTINCT message_id FROM messages)");
		
		await reporter.MainWork("Migrating message reactions...", finishedItems: 1, totalItems: 6);
		await conn.ExecuteAsync("""
		                        CREATE TABLE message_reactions_new (
		                        	message_id  INTEGER NOT NULL,
		                        	emoji_id    INTEGER,
		                        	emoji_name  TEXT,
		                        	emoji_flags INTEGER NOT NULL,
		                        	count       INTEGER NOT NULL,
		                        	FOREIGN KEY (message_id) REFERENCES messages (message_id) ON UPDATE CASCADE ON DELETE CASCADE
		                        )
		                        """);
		await conn.ExecuteAsync("INSERT INTO message_reactions_new (message_id, emoji_id, emoji_name, emoji_flags, count) SELECT message_id, emoji_id, emoji_name, emoji_flags, count FROM message_reactions WHERE message_id IN (SELECT DISTINCT message_id FROM messages)");
		
		await reporter.MainWork("Migrating message edit timestamps...", finishedItems: 2, totalItems: 6);
		await conn.ExecuteAsync("""
		                        CREATE TABLE message_edit_timestamps_new (
		                        	message_id     INTEGER PRIMARY KEY NOT NULL,
		                        	edit_timestamp INTEGER NOT NULL,
		                        	FOREIGN KEY (message_id) REFERENCES messages (message_id) ON UPDATE CASCADE ON DELETE CASCADE
		                        )
		                        """);
		await conn.ExecuteAsync("INSERT INTO message_edit_timestamps_new (message_id, edit_timestamp) SELECT message_id, edit_timestamp FROM message_edit_timestamps WHERE message_id IN (SELECT DISTINCT message_id FROM messages)");
		
		await reporter.MainWork("Migrating message replies...", finishedItems: 3, totalItems: 6);
		await conn.ExecuteAsync("""
		                        CREATE TABLE message_replied_to_new (
		                        	message_id    INTEGER PRIMARY KEY NOT NULL,
		                        	replied_to_id INTEGER NOT NULL,
		                        	FOREIGN KEY (message_id) REFERENCES messages (message_id) ON UPDATE CASCADE ON DELETE CASCADE
		                        )
		                        """);
		await conn.ExecuteAsync("INSERT INTO message_replied_to_new (message_id, replied_to_id) SELECT message_id, replied_to_id FROM message_replied_to WHERE message_id IN (SELECT DISTINCT message_id FROM messages)");
		
		await reporter.MainWork("Applying schema changes...", finishedItems: 4, totalItems: 6);
		
		await conn.ExecuteAsync("DROP TABLE message_embeds");
		await conn.ExecuteAsync("ALTER TABLE message_embeds_new RENAME TO message_embeds");
		await conn.ExecuteAsync("CREATE INDEX embeds_message_ix ON message_embeds(message_id)");
		
		await conn.ExecuteAsync("DROP TABLE message_reactions");
		await conn.ExecuteAsync("ALTER TABLE message_reactions_new RENAME TO message_reactions");
		await conn.ExecuteAsync("CREATE INDEX reactions_message_ix ON message_reactions(message_id)");
		
		await conn.ExecuteAsync("DROP TABLE message_edit_timestamps");
		await conn.ExecuteAsync("ALTER TABLE message_edit_timestamps_new RENAME TO message_edit_timestamps");
		
		await conn.ExecuteAsync("DROP TABLE message_replied_to");
		await conn.ExecuteAsync("ALTER TABLE message_replied_to_new RENAME TO message_replied_to");
		
		await reporter.MainWork("Removing orphaned objects...", finishedItems: 5, totalItems: 6);
		
		Log.Info("Removed orphaned attachments: " + await conn.ExecuteAsync("DELETE FROM attachments WHERE attachment_id NOT IN (SELECT DISTINCT attachment_id FROM message_attachments)"));
		Log.Info("Removed orphaned users: " + await conn.ExecuteAsync("DELETE FROM users WHERE id NOT IN (SELECT DISTINCT sender_id FROM messages)"));
		Log.Info("Removed orphaned channels: " + await conn.ExecuteAsync("DELETE FROM channels WHERE id NOT IN (SELECT DISTINCT channel_id FROM messages)"));
		Log.Info("Removed orphaned servers: " + await conn.ExecuteAsync("DELETE FROM servers WHERE id NOT IN (SELECT DISTINCT server FROM channels)"));
	}
}
