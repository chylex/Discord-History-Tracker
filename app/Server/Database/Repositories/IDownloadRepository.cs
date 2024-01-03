using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Download;

namespace DHT.Server.Database.Repositories;

public interface IDownloadRepository {
	IObservable<long> TotalCount { get; }

	Task AddDownload(DownloadWithData item);

	Task<long> Count(DownloadItemFilter filter, CancellationToken cancellationToken = default);
	
	Task<DownloadStatusStatistics> GetStatistics(DownloadItemFilter nonSkippedFilter, CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<Data.Download> Get();

	Task<DownloadWithData> HydrateWithData(Data.Download download);

	Task<DownloadWithData?> GetSuccessfulDownloadWithData(string normalizedUrl);

	IAsyncEnumerable<DownloadItem> PullPendingDownloadItems(int count, DownloadItemFilter filter, CancellationToken cancellationToken = default);
	
	Task MoveDownloadingItemsBackToQueue(CancellationToken cancellationToken = default);
	
	Task<int> RetryFailed(CancellationToken cancellationToken = default);

	internal sealed class Dummy : IDownloadRepository {
		public IObservable<long> TotalCount { get; } = Observable.Return(0L);

		public Task AddDownload(DownloadWithData item) {
			return Task.CompletedTask;
		}

		public Task<long> Count(DownloadItemFilter filter, CancellationToken cancellationToken) {
			return Task.FromResult(0L);
		}

		public Task<DownloadStatusStatistics> GetStatistics(DownloadItemFilter nonSkippedFilter, CancellationToken cancellationToken) {
			return Task.FromResult(new DownloadStatusStatistics());
		}

		public IAsyncEnumerable<Data.Download> Get() {
			return AsyncEnumerable.Empty<Data.Download>();
		}

		public Task<DownloadWithData> HydrateWithData(Data.Download download) {
			return Task.FromResult(new DownloadWithData(download, Data: null));
		}

		public Task<DownloadWithData?> GetSuccessfulDownloadWithData(string normalizedUrl) {
			return Task.FromResult<DownloadWithData?>(null);
		}

		public IAsyncEnumerable<DownloadItem> PullPendingDownloadItems(int count, DownloadItemFilter filter, CancellationToken cancellationToken) {
			return AsyncEnumerable.Empty<DownloadItem>();
		}

		public Task MoveDownloadingItemsBackToQueue(CancellationToken cancellationToken) {
			return Task.CompletedTask;
		}

		public Task<int> RetryFailed(CancellationToken cancellationToken) {
			return Task.FromResult(0);
		}
	}
}
