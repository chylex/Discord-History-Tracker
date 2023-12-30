using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Download;

namespace DHT.Server.Database.Repositories;

public interface IDownloadRepository {
	Task<long> CountAttachments(AttachmentFilter? filter = null, CancellationToken cancellationToken = default);

	Task AddDownload(Data.Download download);

	Task<DownloadStatusStatistics> GetStatistics(CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<Data.Download> GetWithoutData();

	Task<Data.Download> HydrateWithData(Data.Download download);

	Task<DownloadedAttachment?> GetDownloadedAttachment(string normalizedUrl);

	Task<int> EnqueueDownloadItems(AttachmentFilter? filter = null, CancellationToken cancellationToken = default);

	IAsyncEnumerable<DownloadItem> PullEnqueuedDownloadItems(int count, CancellationToken cancellationToken = default);

	Task RemoveDownloadItems(DownloadItemFilter? filter, FilterRemovalMode mode);

	internal sealed class Dummy : IDownloadRepository {
		public Task<long> CountAttachments(AttachmentFilter? filter, CancellationToken cancellationToken) {
			return Task.FromResult(0L);
		}

		public Task AddDownload(Data.Download download) {
			return Task.CompletedTask;
		}

		public Task<DownloadStatusStatistics> GetStatistics(CancellationToken cancellationToken) {
			return Task.FromResult(new DownloadStatusStatistics());
		}

		public IAsyncEnumerable<Data.Download> GetWithoutData() {
			return AsyncEnumerable.Empty<Data.Download>();
		}

		public Task<Data.Download> HydrateWithData(Data.Download download) {
			return Task.FromResult(download);
		}

		public Task<DownloadedAttachment?> GetDownloadedAttachment(string normalizedUrl) {
			return Task.FromResult<DownloadedAttachment?>(null);
		}

		public Task<int> EnqueueDownloadItems(AttachmentFilter? filter, CancellationToken cancellationToken) {
			return Task.FromResult(0);
		}

		public IAsyncEnumerable<DownloadItem> PullEnqueuedDownloadItems(int count, CancellationToken cancellationToken) {
			return AsyncEnumerable.Empty<DownloadItem>();
		}

		public Task RemoveDownloadItems(DownloadItemFilter? filter, FilterRemovalMode mode) {
			return Task.CompletedTask;
		}
	}
}
