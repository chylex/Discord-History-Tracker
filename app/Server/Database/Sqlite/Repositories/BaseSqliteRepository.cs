using System;
using System.Threading;
using System.Threading.Tasks;
using DHT.Utils.Logging;
using DHT.Utils.Observables;
using DHT.Utils.Tasks;

namespace DHT.Server.Database.Sqlite.Repositories;

abstract class BaseSqliteRepository : IDisposable {
	private readonly ThrottledTask<long> totalCountTask;
	
	public ObservableValue<long> TotalCount { get; } = new (0L);
	
	protected BaseSqliteRepository(Log log) {
		totalCountTask = new ThrottledTask<long>(log, SetTotalCount, TimeSpan.Zero, TaskScheduler.Default);
		UpdateTotalCount();
	}
	
	private Task SetTotalCount(long newCount) {
		TotalCount.Set(newCount);
		return Task.CompletedTask;
	}
	
	public void Dispose() {
		totalCountTask.Dispose();
	}
	
	protected void UpdateTotalCount() {
		totalCountTask.Post(Count);
	}
	
	public abstract Task<long> Count(CancellationToken cancellationToken);
}
