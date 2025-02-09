using System.Collections.Generic;

namespace DHT.Server.Database.Sqlite.Utils;

interface ISqliteAttachedDatabaseCollector {
	IAsyncEnumerable<AttachedDatabase> GetAttachedDatabases(ISqliteConnection conn);
}
