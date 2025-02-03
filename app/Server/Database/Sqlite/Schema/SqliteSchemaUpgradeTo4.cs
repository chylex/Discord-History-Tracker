using System.Threading.Tasks;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite.Schema;

sealed class SqliteSchemaUpgradeTo4 : ISchemaUpgrade {
	async Task ISchemaUpgrade.Run(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter) {
		await reporter.MainWork("Applying schema changes...", finishedItems: 0, totalItems: 1);
		
		await conn.ExecuteAsync("""
		                        CREATE TABLE downloads (
		                        	url    TEXT NOT NULL PRIMARY KEY,
		                        	status INTEGER NOT NULL,
		                        	size   INTEGER NOT NULL,
		                        	blob   BLOB
		                        )
		                        """);
	}
}
