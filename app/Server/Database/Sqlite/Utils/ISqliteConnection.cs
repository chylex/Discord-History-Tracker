using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Utils;

interface ISqliteConnection : IAsyncDisposable {
	SqliteConnection InnerConnection { get; }
	
	Task BeginTransactionAsync();
	Task CommitTransactionAsync();
	Task RollbackTransactionAsync();
	
	void AssignActiveTransaction(SqliteCommand command);
}
