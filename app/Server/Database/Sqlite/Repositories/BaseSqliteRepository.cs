using System;
using System.Threading;
using System.Threading.Tasks;
using DHT.Utils.Tasks;

namespace DHT.Server.Database.Sqlite.Repositories;

abstract class BaseSqliteRepository : IDisposable {
	private readonly ObservableThrottledTask<long> totalCountTask = new (TaskScheduler.Default);
	
	public IObservable<long> TotalCount => totalCountTask;

	protected BaseSqliteRepository() {
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
