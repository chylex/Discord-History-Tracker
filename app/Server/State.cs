using System;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Download;
using DHT.Server.Service;

namespace DHT.Server;

public sealed class State(IDatabaseFile db, int? concurrentDownloads) : IAsyncDisposable {
	public static State Dummy { get; } = new (DummyDatabaseFile.Instance, null);
	
	public IDatabaseFile Db { get; } = db;
	public Downloader Downloader { get; } = new (db, concurrentDownloads);
	public ServerManager Server { get; } = new (db);

	public async ValueTask DisposeAsync() {
		await Downloader.Stop();
		await Server.Stop();
		await Db.DisposeAsync();
	}
}
