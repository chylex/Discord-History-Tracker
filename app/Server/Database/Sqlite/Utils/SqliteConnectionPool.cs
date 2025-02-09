using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DHT.Utils.Collections;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Utils;

sealed class SqliteConnectionPool : IAsyncDisposable {
	public static async Task<SqliteConnectionPool> Create(SqliteConnectionStringBuilder connectionStringBuilder, int poolSize, ISqliteAttachedDatabaseCollector attachedDatabaseCollector) {
		var pool = new SqliteConnectionPool(poolSize, attachedDatabaseCollector);
		await pool.InitializePooledConnections(connectionStringBuilder);
		return pool;
	}
	
	private static string GetConnectionString(SqliteConnectionStringBuilder connectionStringBuilder) {
		connectionStringBuilder.Pooling = false;
		return connectionStringBuilder.ToString();
	}
	
	private readonly ISqliteAttachedDatabaseCollector attachedDatabaseCollector;
	
	private readonly int poolSize;
	private readonly List<PooledConnection> all;
	private readonly ConcurrentPool<PooledConnection> free;
	
	private readonly CancellationTokenSource disposalTokenSource = new ();
	private readonly CancellationToken disposalToken;
	
	private SqliteConnectionPool(int poolSize, ISqliteAttachedDatabaseCollector attachedDatabaseCollector) {
		this.attachedDatabaseCollector = attachedDatabaseCollector;
		
		this.poolSize = poolSize;
		this.all = new List<PooledConnection>(poolSize);
		this.free = new ConcurrentPool<PooledConnection>(poolSize);
		
		this.disposalToken = disposalTokenSource.Token;
	}
	
	private async Task InitializePooledConnections(SqliteConnectionStringBuilder connectionStringBuilder) {
		string connectionString = GetConnectionString(connectionStringBuilder);
		
		List<string> additionalSql = [];
		HashSet<string> attachedSchemas = [];
		
		for (int i = 0; i < poolSize; i++) {
			SqliteConnection conn = new SqliteConnection(connectionString);
			conn.Open();
			
			PooledConnection pooledConnection = new PooledConnection(this, conn, attachedSchemas);
			
			if (i == 0) {
				await foreach ((string path, string schema) in attachedDatabaseCollector.GetAttachedDatabases(pooledConnection).WithCancellation(disposalToken)) {
					additionalSql.Add("ATTACH DATABASE '" + EscapeQuotes(path) + "' AS '" + EscapeQuotes(schema) + "'");
					attachedSchemas.Add(schema);
				}
			}
			
			await pooledConnection.ExecuteAsync("PRAGMA journal_mode=WAL", disposalToken);
			await pooledConnection.ExecuteAsync("PRAGMA foreign_keys=ON", disposalToken);
			
			foreach (string sql in additionalSql) {
				await pooledConnection.ExecuteAsync(sql, disposalToken);
			}
			
			all.Add(pooledConnection);
			await free.Push(pooledConnection, disposalToken);
		}
	}
	
	private static string EscapeQuotes(string str) {
		return str.Replace("'", "''");
	}
	
	public async Task<ISqliteConnection> Take() {
		return await free.Pop(disposalToken);
	}
	
	private async Task Return(PooledConnection conn) {
		await free.Push(conn, disposalToken);
	}
	
	public async ValueTask DisposeAsync() {
		if (disposalToken.IsCancellationRequested) {
			return;
		}
		
		await disposalTokenSource.CancelAsync();
		
		foreach (PooledConnection conn in all) {
			await conn.InnerConnection.CloseAsync();
			await conn.InnerConnection.DisposeAsync();
		}
		
		disposalTokenSource.Dispose();
	}
	
	private sealed class PooledConnection(SqliteConnectionPool pool, SqliteConnection conn, HashSet<string> attachedSchemas) : ISqliteConnection {
		public SqliteConnection InnerConnection { get; } = conn;
		private HashSet<string> AttachedSchemas { get; } = attachedSchemas;
		
		private DbTransaction? activeTransaction;
		
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
		
		public bool HasAttachedDatabase(string schema) {
			return AttachedSchemas.Contains(schema);
		}
		
		public async ValueTask DisposeAsync() {
			if (activeTransaction != null) {
				await RollbackTransactionAsync();
			}
			
			await pool.Return(this);
		}
	}
}
