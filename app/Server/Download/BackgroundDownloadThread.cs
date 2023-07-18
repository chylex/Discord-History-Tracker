using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using DHT.Server.Database;
using DHT.Utils.Logging;
using DHT.Utils.Models;

namespace DHT.Server.Download;

public sealed class BackgroundDownloadThread : BaseModel {
	private static readonly Log Log = Log.ForType<BackgroundDownloadThread>();

	public event EventHandler<DownloadItem>? OnItemFinished {
		add => parameters.OnItemFinished += value;
		remove => parameters.OnItemFinished -= value;
	}

	public event EventHandler? OnServerStopped {
		add => parameters.OnServerStopped += value;
		remove => parameters.OnServerStopped -= value;
	}

	private readonly CancellationTokenSource cancellationTokenSource;
	private readonly ThreadInstance.Parameters parameters;

	public BackgroundDownloadThread(IDatabaseFile db) {
		this.cancellationTokenSource = new CancellationTokenSource();
		this.parameters = new ThreadInstance.Parameters(db, cancellationTokenSource);

		var thread = new Thread(new ThreadInstance().Work) {
			Name = "DHT download thread"
		};

		thread.Start(parameters);
	}

	public void StopThread() {
		try {
			cancellationTokenSource.Cancel();
		} catch (ObjectDisposedException) {
			Log.Warn("Attempted to stop background download thread after the cancellation token has been disposed.");
		}
	}

	private sealed class ThreadInstance {
		private const int QueueSize = 32;

		public sealed class Parameters {
			public event EventHandler<DownloadItem>? OnItemFinished;
			public event EventHandler? OnServerStopped;

			public IDatabaseFile Db { get; }
			public CancellationTokenSource CancellationTokenSource { get; }

			public Parameters(IDatabaseFile db, CancellationTokenSource cancellationTokenSource) {
				Db = db;
				CancellationTokenSource = cancellationTokenSource;
			}

			public void FireOnItemFinished(DownloadItem item) {
				OnItemFinished?.Invoke(null, item);
			}

			public void FireOnServerStopped() {
				OnServerStopped?.Invoke(null, EventArgs.Empty);
			}
		}

		private readonly HttpClient client = new ();

		public ThreadInstance() {
			client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.51 Safari/537.36");
		}

		public async void Work(object? obj) {
			var parameters = (Parameters) obj!;

			var cancellationTokenSource = parameters.CancellationTokenSource;
			var cancellationToken = cancellationTokenSource.Token;

			var db = parameters.Db;
			var queue = new ConcurrentQueue<DownloadItem>();

			try {
				while (!cancellationToken.IsCancellationRequested) {
					FillQueue(db, queue, cancellationToken);

					while (!cancellationToken.IsCancellationRequested && queue.TryDequeue(out var item)) {
						var url = item.Url;
						Log.Debug("Downloading " + url + "...");

						try {
							db.AddDownload(Data.Download.NewSuccess(url, await client.GetByteArrayAsync(url, cancellationToken)));
						} catch (HttpRequestException e) {
							db.AddDownload(Data.Download.NewFailure(url, e.StatusCode, item.Size));
							Log.Error(e);
						} catch (Exception e) {
							db.AddDownload(Data.Download.NewFailure(url, null, item.Size));
							Log.Error(e);
						} finally {
							parameters.FireOnItemFinished(item);
						}
					}
				}
			} catch (OperationCanceledException) {
				//
			} catch (ObjectDisposedException) {
				//
			} finally {
				cancellationTokenSource.Dispose();
				parameters.FireOnServerStopped();
			}
		}

		private static void FillQueue(IDatabaseFile db, ConcurrentQueue<DownloadItem> queue, CancellationToken cancellationToken) {
			while (!cancellationToken.IsCancellationRequested && queue.IsEmpty) {
				var newItems = db.GetEnqueuedDownloadItems(QueueSize);
				if (newItems.Count == 0) {
					Thread.Sleep(TimeSpan.FromMilliseconds(50));
				}
				else {
					foreach (var item in newItems) {
						queue.Enqueue(item);
					}
				}
			}
		}
	}
}
