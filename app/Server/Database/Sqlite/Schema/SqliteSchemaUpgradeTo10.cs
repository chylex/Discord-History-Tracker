using System.Threading.Tasks;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite.Schema;

sealed class SqliteSchemaUpgradeTo10 : ISchemaUpgrade {
	async Task ISchemaUpgrade.Run(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.MainWork("Migrating message embeds...", 0, 5);
		await conn.ExecuteAsync("""
		                        CREATE TABLE message_embeds_new (
		                        	message_id INTEGER NOT NULL,
		                        	json       TEXT NOT NULL,
		                        	FOREIGN KEY (message_id) REFERENCES messages (message_id) ON UPDATE CASCADE ON DELETE CASCADE
		                        )
		                        """);
		await conn.ExecuteAsync("INSERT INTO message_embeds_new (message_id, json) SELECT message_id, json FROM message_embeds WHERE message_id IN (SELECT DISTINCT message_id FROM messages)");
		
		await reporter.MainWork("Migrating message reactions...", 1, 5);
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
		
		await reporter.MainWork("Migrating message edit timestamps...", 2, 5);
		await conn.ExecuteAsync("""
		                        CREATE TABLE message_edit_timestamps_new (
		                        	message_id     INTEGER PRIMARY KEY NOT NULL,
		                        	edit_timestamp INTEGER NOT NULL,
		                        	FOREIGN KEY (message_id) REFERENCES messages (message_id) ON UPDATE CASCADE ON DELETE CASCADE
		                        )
		                        """);
		await conn.ExecuteAsync("INSERT INTO message_edit_timestamps_new (message_id, edit_timestamp) SELECT message_id, edit_timestamp FROM message_edit_timestamps WHERE message_id IN (SELECT DISTINCT message_id FROM messages)");
		
		await reporter.MainWork("Migrating message replies...", 3, 5);
		await conn.ExecuteAsync("""
		                        CREATE TABLE message_replied_to_new (
		                        	message_id    INTEGER PRIMARY KEY NOT NULL,
		                        	replied_to_id INTEGER NOT NULL,
		                        	FOREIGN KEY (message_id) REFERENCES messages (message_id) ON UPDATE CASCADE ON DELETE CASCADE
		                        )
		                        """);
		await conn.ExecuteAsync("INSERT INTO message_replied_to_new (message_id, replied_to_id) SELECT message_id, replied_to_id FROM message_replied_to WHERE message_id IN (SELECT DISTINCT message_id FROM messages)");
		
		await reporter.MainWork("Applying schema changes...", 4, 5);
		
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
	}
}
