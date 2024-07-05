using System.Threading.Tasks;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite.Schema;

sealed class SqliteSchemaUpgradeTo9 : ISchemaUpgrade {
	async Task ISchemaUpgrade.Run(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.MainWork("Applying schema changes...", 0, 3);
		await SqliteSchema.CreateMessageAttachmentsTable(conn);
		
		await reporter.MainWork("Migrating message attachments...", 1, 3);
		await conn.ExecuteAsync("INSERT INTO message_attachments (message_id, attachment_id) SELECT message_id, attachment_id FROM attachments");
		
		await reporter.MainWork("Applying schema changes...", 2, 3);
		await conn.ExecuteAsync("DROP INDEX attachments_message_ix");
		await conn.ExecuteAsync("ALTER TABLE attachments DROP COLUMN message_id");
		
		await conn.ExecuteAsync("ALTER TABLE embeds RENAME TO message_embeds");
		await conn.ExecuteAsync("ALTER TABLE edit_timestamps RENAME TO message_edit_timestamps");
		await conn.ExecuteAsync("ALTER TABLE replied_to RENAME TO message_replied_to");
		await conn.ExecuteAsync("ALTER TABLE reactions RENAME TO message_reactions");
	}
}
