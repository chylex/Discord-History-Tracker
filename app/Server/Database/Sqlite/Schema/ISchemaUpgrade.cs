using System.Threading.Tasks;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite.Schema;

interface ISchemaUpgrade {
	Task Run(ISqliteConnection conn, ISchemaUpgradeCallbacks.IProgressReporter reporter);
}
