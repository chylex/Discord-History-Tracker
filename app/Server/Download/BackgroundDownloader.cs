using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Utils.Logging;
using DHT.Utils.Models;
using DHT.Utils.Tasks;

namespace DHT.Server.Download;

public sealed class BackgroundDownloader : BaseModel {
	private static readonly Log Log = Log.ForType<BackgroundDownloader>();

	private const int DownloadTasks = 4;
	private const int QueueSize = 25;
	private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.51 Safari/537.36";

	public event EventHandler<DownloadItem>? OnItemFinished;

	private readonly Channel<DownloadItem> downloadQueue = Channel.CreateBounded<DownloadItem>(new BoundedChannelOptions(QueueSize) {
		SingleReader = false,
		SingleWriter = true,
		AllowSynchronousContinuations = false,
		FullMode = BoundedChannelFullMode.Wait
	});

	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private readonly CancellationToken cancellationToken;

	private readonly IDatabaseFile db;
	private readonly Task queueWriterTask;
	private readonly Task[] downloadTasks;

	public BackgroundDownloader(IDatabaseFile db) {
		this.cancellationToken = cancellationTokenSource.Token;
		this.db = db;
		this.queueWriterTask = Task.Run(RunQueueWriterTask);
		this.downloadTasks = Enumerable.Range(1, DownloadTasks).Select(taskIndex => Task.Run(() => RunDownloadTask(taskIndex))).ToArray();
	}

	private async Task RunQueueWriterTask() {
		while (await downloadQueue.Writer.WaitToWriteAsync(cancellationToken)) {
			var newItems = db.PullEnqueuedDownloadItems(QueueSize);
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
		var log = Log.ForType<BackgroundDownloader>("Task " + taskIndex);

		var client = new HttpClient();
		client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
		client.Timeout = TimeSpan.FromSeconds(30);

		while (!cancellationToken.IsCancellationRequested) {
			var item = await downloadQueue.Reader.ReadAsync(cancellationToken);
			log.Debug("Downloading " + item.DownloadUrl + "...");

			try {
				var downloadedBytes = await client.GetByteArrayAsync(item.DownloadUrl, cancellationToken);
				db.AddDownload(Data.Download.NewSuccess(item, downloadedBytes));
			} catch (OperationCanceledException) {
				// Ignore.
			} catch (HttpRequestException e) {
				db.AddDownload(Data.Download.NewFailure(item, e.StatusCode, item.Size));
				log.Error(e);
			} catch (Exception e) {
				db.AddDownload(Data.Download.NewFailure(item, null, item.Size));
				log.Error(e);
			} finally {
				try {
					OnItemFinished?.Invoke(this, item);
				} catch (Exception e) {
					log.Error("Caught exception in event handler: " + e);
				}
			}
		}
	}

	public async Task Stop() {
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
		}
	}
}
