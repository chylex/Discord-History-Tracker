using System;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Utils;

class CustomSqliteConnection : ISqliteConnection {
	internal static async Task<CustomSqliteConnection> OpenUnpooled(SqliteConnectionStringFactory connectionStringFactory) {
		SqliteConnection conn = new SqliteConnection(connectionStringFactory.Create());
		await conn.OpenAsync();
		
		CustomSqliteConnection custom = new CustomSqliteConnection(connectionStringFactory, conn);
		await custom.Setup(CancellationToken.None);
		
		return custom;
	}
	
	internal SqliteConnectionStringFactory ConnectionStringFactory { get; }
	public SqliteConnection InnerConnection { get; }
	
	private ImmutableHashSet<string> attachedDatabases = [];
	private DbTransaction? activeTransaction;
	
	protected CustomSqliteConnection(SqliteConnectionStringFactory connectionStringFactory, SqliteConnection innerConnection) {
		this.ConnectionStringFactory = connectionStringFactory;
		this.InnerConnection = innerConnection;
	}
	
	internal async Task Setup(CancellationToken cancellationToken) {
		await this.ExecuteAsync("PRAGMA journal_mode=WAL", cancellationToken);
		await this.ExecuteAsync("PRAGMA foreign_keys=ON", cancellationToken);
	}
	
	[SuppressMessage("ReSharper", "WithExpressionModifiesAllMembers")]
	internal async Task AttachDatabase(AttachedDatabase attachedDatabase) {
		await this.ExecuteAsync("ATTACH DATABASE '" + attachedDatabase.Path.Replace("'", "''") + "' AS " + attachedDatabase.Schema);
		ImmutableInterlocked.Update(ref attachedDatabases, static (set, schema) => set.Add(schema), attachedDatabase.Schema);
	}
	
	public bool HasAttachedDatabase(string schema) {
		return attachedDatabases.Contains(schema);
	}
	
	public async Task BeginTransactionAsync() {
		if (activeTransaction != null) {
			throw new InvalidOperationException("A transaction is already active.");
		}
		
		activeTransaction = await InnerConnection.BeginTransactionAsync();
	}
	
	public async Task CommitTransactionAsync() {
		if (activeTransaction == null) {
			throw new InvalidOperationException("No active transaction to commit.");
		}
		
		await activeTransaction.CommitAsync();
		await activeTransaction.DisposeAsync();
		activeTransaction = null;
	}
	
	public async Task RollbackTransactionAsync() {
		if (activeTransaction == null) {
			throw new InvalidOperationException("No active transaction to rollback.");
		}
		
		await activeTransaction.RollbackAsync();
		await activeTransaction.DisposeAsync();
		activeTransaction = null;
	}
	
	public void AssignActiveTransaction(SqliteCommand command) {
		command.Transaction = (SqliteTransaction?) activeTransaction;
	}
	
	public async ValueTask DisposeAsync() {
		if (activeTransaction != null) {
			await RollbackTransactionAsync();
		}
		
		await DisposeConnection();
	}
	
	private protected virtual async ValueTask DisposeConnection() {
		await InnerConnection.DisposeAsync();
	}
}
