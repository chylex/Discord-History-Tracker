using System;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Download;
using DHT.Server.Service;

namespace DHT.Server;

public sealed class State : IAsyncDisposable {
	public static State Dummy { get; } = new (DummyDatabaseFile.Instance);
	
	public IDatabaseFile Db { get; }
	public Downloader Downloader { get; }
	public ServerManager Server { get; }

	public State(IDatabaseFile db) {
		Db = db;
		Downloader = new Downloader(db);
		Server = new ServerManager(db);
	}

	public async ValueTask DisposeAsync() {
		await Downloader.Stop();
		await Server.Stop();
		Db.Dispose();
	}
}
