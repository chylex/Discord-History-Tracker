using System.Threading.Tasks;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite.Schema;

sealed class SqliteSchemaUpgradeTo11 : ISchemaUpgrade {
	async Task ISchemaUpgrade.Run(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.MainWork("Applying schema changes...", finishedItems: 0, totalItems: 1);
		await conn.ExecuteAsync("ALTER TABLE servers ADD icon_hash TEXT");
	}
}
