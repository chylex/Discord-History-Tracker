using System.Threading.Tasks;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite.Schema;

sealed class SqliteSchemaUpgradeTo8 : ISchemaUpgrade {
	async Task ISchemaUpgrade.Run(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.MainWork("Applying schema changes...", 0, 1);
		await conn.ExecuteAsync("ALTER TABLE users ADD display_name TEXT");
	}
}
