using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DHT.Utils.Collections;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Utils;

sealed class SqliteConnectionPool : IAsyncDisposable {
	public static async Task<SqliteConnectionPool> Create(SqliteConnectionStringBuilder connectionStringBuilder, int poolSize) {
		var pool = new SqliteConnectionPool(poolSize);
		await pool.InitializePooledConnections(connectionStringBuilder);
		return pool;
	}

	private static string GetConnectionString(SqliteConnectionStringBuilder connectionStringBuilder) {
		connectionStringBuilder.Pooling = false;
		return connectionStringBuilder.ToString();
	}

	private readonly int poolSize;
	private readonly List<PooledConnection> all;
	private readonly ConcurrentPool<PooledConnection> free;

	private readonly CancellationTokenSource disposalTokenSource = new ();
	private readonly CancellationToken disposalToken;

	private SqliteConnectionPool(int poolSize) {
		this.poolSize = poolSize;
		this.all = new List<PooledConnection>(poolSize);
		this.free = new ConcurrentPool<PooledConnection>(poolSize);
		this.disposalToken = disposalTokenSource.Token;
	}

	private async Task InitializePooledConnections(SqliteConnectionStringBuilder connectionStringBuilder) {
		var connectionString = GetConnectionString(connectionStringBuilder);

		for (int i = 0; i < poolSize; i++) {
			var conn = new SqliteConnection(connectionString);
			conn.Open();

			var pooledConnection = new PooledConnection(this, conn);

			await pooledConnection.ExecuteAsync("PRAGMA journal_mode=WAL", disposalToken);
			await pooledConnection.ExecuteAsync("PRAGMA foreign_keys=ON", disposalToken);

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
		
		foreach (var conn in all) {
			await conn.InnerConnection.CloseAsync();
			await conn.InnerConnection.DisposeAsync();
		}
		
		disposalTokenSource.Dispose();
	}

	private sealed class PooledConnection : ISqliteConnection {
		public SqliteConnection InnerConnection { get; }

		private readonly SqliteConnectionPool pool;

		public PooledConnection(SqliteConnectionPool pool, SqliteConnection conn) {
			this.pool = pool;
			this.InnerConnection = conn;
		}

		public async ValueTask DisposeAsync() {
			await pool.Return(this);
		}
	}
}
