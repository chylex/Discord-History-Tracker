using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Utils.Logging;
using DHT.Utils.Tasks;

namespace DHT.Server.Database.Sqlite.Repositories;

abstract class BaseSqliteRepository : IDisposable {
	private readonly ObservableThrottledTask<long> totalCountTask;

	public IObservable<long> TotalCount { get; }

	protected BaseSqliteRepository(Log log) {
		totalCountTask = new ObservableThrottledTask<long>(log, TaskScheduler.Default);
		TotalCount = totalCountTask.DistinctUntilChanged();
		UpdateTotalCount();
	}

	public void Dispose() {
		totalCountTask.Dispose();
	}

	protected void UpdateTotalCount() {
		totalCountTask.Post(Count);
	}

	public abstract Task<long> Count(CancellationToken cancellationToken);
}
