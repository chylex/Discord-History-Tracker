using System;
using System.Collections.Generic;
using System.IO;
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
	
	Task AddDownload(Data.Download item, Stream? stream);
	
	Task<long> Count(DownloadItemFilter? filter = null, CancellationToken cancellationToken = default);
	
	Task<DownloadStatusStatistics> GetStatistics(DownloadItemFilter nonSkippedFilter, CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<Data.Download> Get(DownloadItemFilter? filter = null);
	
	Task<bool> GetDownloadData(string normalizedUrl, Func<Stream, Task> dataProcessor);
	
	Task<bool> GetSuccessfulDownloadWithData(string normalizedUrl, Func<Data.Download, Stream, CancellationToken, Task> dataProcessor, CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<DownloadItem> PullPendingDownloadItems(int count, DownloadItemFilter filter, CancellationToken cancellationToken = default);
	
	Task MoveDownloadingItemsBackToQueue(CancellationToken cancellationToken = default);
	
	Task<int> RetryFailed(CancellationToken cancellationToken = default);
	
	Task Remove(ICollection<string> normalizedUrls);
	
	IAsyncEnumerable<FileUrl> FindReachableFiles(CancellationToken cancellationToken = default);
	
	internal sealed class Dummy : IDownloadRepository {
		public IObservable<long> TotalCount { get; } = Observable.Return(0L);
		
		public Task AddDownload(Data.Download item, Stream? stream) {
			return Task.CompletedTask;
		}
		
		public Task<long> Count(DownloadItemFilter? filter, CancellationToken cancellationToken) {
			return Task.FromResult(0L);
		}
		
		public Task<DownloadStatusStatistics> GetStatistics(DownloadItemFilter nonSkippedFilter, CancellationToken cancellationToken) {
			return Task.FromResult(new DownloadStatusStatistics());
		}
		
		public IAsyncEnumerable<Data.Download> Get(DownloadItemFilter? filter) {
			return AsyncEnumerable.Empty<Data.Download>();
		}
		
		public Task<bool> GetDownloadData(string normalizedUrl, Func<Stream, Task> dataProcessor) {
			return Task.FromResult(false);
		}
		
		public Task<bool> GetSuccessfulDownloadWithData(string normalizedUrl, Func<Data.Download, Stream, CancellationToken, Task> dataProcessor, CancellationToken cancellationToken) {
			return Task.FromResult(false);
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
		
		public Task Remove(ICollection<string> normalizedUrls) {
			return Task.CompletedTask;
		}
		
		public IAsyncEnumerable<FileUrl> FindReachableFiles(CancellationToken cancellationToken) {
			return AsyncEnumerable.Empty<FileUrl>();
		}
	}
}
