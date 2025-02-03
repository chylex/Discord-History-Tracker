using System;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Download;
using DHT.Server.Service;
using DHT.Server.Service.Viewer;

namespace DHT.Server;

public sealed class State : IAsyncDisposable {
	public static State Dummy { get; } = new (DummyDatabaseFile.Instance, concurrentDownloads: null);
	
	public IDatabaseFile Db { get; }
	public Downloader Downloader { get; }
	public ViewerSessions ViewerSessions { get; }
	public ServerManager Server { get; }
	
	public State(IDatabaseFile db, int? concurrentDownloads) {
		Db = db;
		Downloader = new Downloader(db, concurrentDownloads);
		ViewerSessions = new ViewerSessions();
		Server = new ServerManager(db, ViewerSessions);
	}
	
	public async ValueTask DisposeAsync() {
		await Downloader.Stop();
		await Server.Stop();
		await Db.DisposeAsync();
		ViewerSessions.Dispose();
	}
}
