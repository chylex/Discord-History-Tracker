using System;
using System.Linq;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Utils.Logging;
using DHT.Utils.Tasks;

namespace DHT.Server.Download;

sealed class DownloaderTask : IAsyncDisposable {
	private static readonly Log Log = Log.ForType<DownloaderTask>();

	private const int DownloadTasks = 4;
	private const int QueueSize = 25;
	private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

	private readonly Channel<DownloadItem> downloadQueue = Channel.CreateBounded<DownloadItem>(new BoundedChannelOptions(QueueSize) {
		SingleReader = false,
		SingleWriter = true,
		AllowSynchronousContinuations = false,
		FullMode = BoundedChannelFullMode.Wait
	});

	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private readonly CancellationToken cancellationToken;

	private readonly IDatabaseFile db;
	private readonly DownloadItemFilter filter;
	private readonly ISubject<DownloadItem> finishedItemPublisher = Subject.Synchronize(new Subject<DownloadItem>());

	private readonly Task queueWriterTask;
	private readonly Task[] downloadTasks;

	public IObservable<DownloadItem> FinishedItems => finishedItemPublisher;

	internal DownloaderTask(IDatabaseFile db, DownloadItemFilter filter) {
		this.db = db;
		this.filter = filter;
		this.cancellationToken = cancellationTokenSource.Token;
		this.queueWriterTask = Task.Run(RunQueueWriterTask);
		this.downloadTasks = Enumerable.Range(1, DownloadTasks).Select(taskIndex => Task.Run(() => RunDownloadTask(taskIndex))).ToArray();
	}

	private async Task RunQueueWriterTask() {
		while (await downloadQueue.Writer.WaitToWriteAsync(cancellationToken)) {
			var newItems = await db.Downloads.PullPendingDownloadItems(QueueSize, filter, cancellationToken).ToListAsync(cancellationToken);
			if (newItems.Count == 0) {
				await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
				continue;
			}

			foreach (var newItem in newItems) {
				await downloadQueue.Writer.WriteAsync(newItem, cancellationToken);
			}
		}
	}

	private async Task RunDownloadTask(int taskIndex) {
		var log = Log.ForType<DownloaderTask>("Task " + taskIndex);

		var client = new HttpClient();
		client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
		client.Timeout = TimeSpan.FromSeconds(30);

		while (!cancellationToken.IsCancellationRequested) {
			var item = await downloadQueue.Reader.ReadAsync(cancellationToken);
			log.Debug("Downloading " + item.DownloadUrl + "...");

			try {
				var downloadedBytes = await client.GetByteArrayAsync(item.DownloadUrl, cancellationToken);
				await db.Downloads.AddDownload(item.ToSuccess(downloadedBytes));
			} catch (OperationCanceledException) {
				// Ignore.
			} catch (HttpRequestException e) {
				await db.Downloads.AddDownload(item.ToFailure(e.StatusCode));
				log.Error(e);
			} catch (Exception e) {
				await db.Downloads.AddDownload(item.ToFailure());
				log.Error(e);
			} finally {
				try {
					finishedItemPublisher.OnNext(item);
				} catch (Exception e) {
					log.Error("Caught exception in event handler: " + e);
				}
			}
		}
	}

	public async ValueTask DisposeAsync() {
		try {
			await cancellationTokenSource.CancelAsync();
		} catch (Exception) {
			Log.Warn("Attempted to stop background download twice.");
			return;
		}

		downloadQueue.Writer.Complete();

		try {
			await queueWriterTask.WaitIgnoringCancellation();
			await Task.WhenAll(downloadTasks).WaitIgnoringCancellation();
		} finally {
			cancellationTokenSource.Dispose();
			finishedItemPublisher.OnCompleted();
		}
	}
}
