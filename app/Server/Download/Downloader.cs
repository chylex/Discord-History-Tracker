using System;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;

namespace DHT.Server.Download;

public sealed class Downloader {
	private DownloaderTask? current;
	public bool IsDownloading => current != null;
	
	public event EventHandler<DownloadItem>? OnItemFinished;
	
	private readonly IDatabaseFile db;
	private readonly SemaphoreSlim semaphore = new (1, 1);
	
	internal Downloader(IDatabaseFile db) {
		this.db = db;
	}

	public async Task Start() {
		await semaphore.WaitAsync();
		try {
			if (current == null) {
				current = new DownloaderTask(db);
				current.OnItemFinished += DelegateOnItemFinished;
			}
		} finally {
			semaphore.Release();
		}
	}

	public async Task Stop() {
		await semaphore.WaitAsync();
		try {
			if (current != null) {
				await current.Stop();
				current.OnItemFinished -= DelegateOnItemFinished;
				current = null;
			}
		} finally {
			semaphore.Release();
		}
	}

	private void DelegateOnItemFinished(object? sender, DownloadItem e) {
		OnItemFinished?.Invoke(this, e);
	}
}
