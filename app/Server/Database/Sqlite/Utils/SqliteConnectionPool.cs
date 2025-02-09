using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Utils.Collections;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Utils;

sealed class SqliteConnectionPool : IAsyncDisposable {
	public static async Task<SqliteConnectionPool> Create(SqliteConnectionStringFactory connectionStringFactory, int poolSize, ISqliteAttachedDatabaseCollector attachedDatabaseCollector) {
		SqliteConnectionPool pool = new SqliteConnectionPool(poolSize, attachedDatabaseCollector);
		await pool.InitializePooledConnections(connectionStringFactory);
		return pool;
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
	
	private async Task InitializePooledConnections(SqliteConnectionStringFactory connectionStringFactory) {
		string connectionString = connectionStringFactory.Create();
		
		List<AttachedDatabase> attachedDatabases = [];
		
		for (int i = 0; i < poolSize; i++) {
			SqliteConnection innerConnection = new SqliteConnection(connectionString);
			await innerConnection.OpenAsync(disposalToken);
			
			PooledConnection pooledConnection = new PooledConnection(this, connectionStringFactory, innerConnection);
			await pooledConnection.Setup(disposalToken);
			
			if (i == 0) {
				attachedDatabases = await attachedDatabaseCollector.GetAttachedDatabases(pooledConnection).ToListAsync(disposalToken);
			}
			
			foreach (var attachedDatabase in attachedDatabases) {
				await pooledConnection.AttachDatabase(attachedDatabase);
			}
			
			all.Add(pooledConnection);
			await free.Push(pooledConnection, disposalToken);
		}
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
	
	private sealed class PooledConnection(SqliteConnectionPool pool, SqliteConnectionStringFactory connectionStringFactory, SqliteConnection innerConnection) : CustomSqliteConnection(connectionStringFactory, innerConnection) {
		private protected override async ValueTask DisposeConnection() {
			await pool.Return(this);
		}
	}
}
