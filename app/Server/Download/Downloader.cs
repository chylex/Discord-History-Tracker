using System;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;

namespace DHT.Server.Download;

public sealed class Downloader {
	private DownloaderTask? current;
	public bool IsDownloading => current != null;
	
	private readonly IDatabaseFile db;
	private readonly SemaphoreSlim semaphore = new (1, 1);
	
	internal Downloader(IDatabaseFile db) {
		this.db = db;
	}

	public async Task<IObservable<DownloadItem>> Start() {
		await semaphore.WaitAsync();
		try {
			current ??= new DownloaderTask(db);
			return current.FinishedItems;
		} finally {
			semaphore.Release();
		}
	}

	public async Task Stop() {
		await semaphore.WaitAsync();
		try {
			if (current != null) {
				await current.DisposeAsync();
				current = null;
			}
		} finally {
			semaphore.Release();
		}
	}
}
