using System;
using System.Threading;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite {
	sealed class SqliteMessageStatisticsThread : IDisposable {
		private readonly SqliteConnectionPool pool;
		private readonly Action<ISqliteConnection> action;
		
		private readonly CancellationTokenSource cancellationTokenSource = new();
		private readonly CancellationToken cancellationToken;
		
		private readonly AutoResetEvent requestEvent = new (false);

		public SqliteMessageStatisticsThread(SqliteConnectionPool pool, Action<ISqliteConnection> action) {
			this.pool = pool;
			this.action = action;
			
			this.cancellationToken = cancellationTokenSource.Token;
			
			var thread = new Thread(RunThread) {
				Name = "DHT message statistics thread",
				IsBackground = true
			};
			thread.Start();
		}

		public void Dispose() {
			try {
				cancellationTokenSource.Cancel();
			} catch (ObjectDisposedException) {}
		}

		public void RequestUpdate() {
			try {
				requestEvent.Set();
			} catch (ObjectDisposedException) {}
		}

		private void RunThread() {
			try {
				while (!cancellationToken.IsCancellationRequested) {
					if (requestEvent.WaitOne(TimeSpan.FromMilliseconds(100))) {
						using var conn = pool.Take();
						action(conn);
					}
				}
			} finally {
				cancellationTokenSource.Dispose();
				requestEvent.Dispose();
			}
		}
	}
}
