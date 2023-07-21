using System;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Service;

namespace DHT.Server; 

public sealed class Session : IAsyncDisposable {
	public static async Task<Session> Start(IDatabaseFile db, SessionSettings settings) {
		var session = new Session(db);
		await session.Start(settings);
		return session;
	}

	private readonly IDatabaseFile db;
	private readonly AppWebServer webServer;
	
	private Session(IDatabaseFile db) {
		this.db = db;
		this.webServer = new AppWebServer(db);
	}

	private async Task Start(SessionSettings settings) {
		await webServer.Start(settings.ServerPort, settings.ServerToken);
	}

	public async Task ChangeSettings(SessionSettings settings) {
		await webServer.Start(settings.ServerPort, settings.ServerToken);
	}

	public async ValueTask DisposeAsync() {
		await webServer.Stop();
	}
}
