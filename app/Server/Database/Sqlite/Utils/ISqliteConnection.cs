using System;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Utils;

interface ISqliteConnection : IDisposable {
	SqliteConnection InnerConnection { get; }
}
