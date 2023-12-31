using System;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Utils;

interface ISqliteConnection : IAsyncDisposable {
	SqliteConnection InnerConnection { get; }
}
