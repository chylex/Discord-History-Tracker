using System.Threading.Tasks;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite.Schema;

sealed class SqliteSchemaUpgradeTo3 : ISchemaUpgrade {
	async Task ISchemaUpgrade.Run(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.MainWork("Applying schema changes...", 0, 1);
		
		await conn.ExecuteAsync("""
		                        CREATE TABLE edit_timestamps (
		                        	message_id     INTEGER PRIMARY KEY NOT NULL,
		                        	edit_timestamp INTEGER NOT NULL
		                        )
		                        """);
		
		await conn.ExecuteAsync("""
		                        CREATE TABLE replied_to (
		                        	message_id    INTEGER PRIMARY KEY NOT NULL,
		                        	replied_to_id INTEGER NOT NULL
		                        )
		                        """);
		
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
		
		await reporter.MainWork("Vacuuming the database...", 1, 1);
		await conn.ExecuteAsync("VACUUM");
	}
}
