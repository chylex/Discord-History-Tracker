using System;
using DHT.Server;
using DHT.Server.Service;

namespace DHT.Desktop.Server;

sealed class ServerManager : IDisposable {
	public static ushort Port { get; set; } = ServerUtils.FindAvailablePort(50000, 60000);
	public static string Token { get; set; } = ServerUtils.GenerateRandomToken(20);

	private static ServerManager? instance;

	public bool IsRunning => ServerLauncher.IsRunning;

	private readonly State state;

	public ServerManager(State state) {
		if (state != State.Dummy) {
			if (instance != null) {
				throw new InvalidOperationException("Only one instance of ServerManager can exist at the same time!");
			}

			instance = this;
		}

		this.state = state;
	}

	public void Launch() {
		ServerLauncher.Relaunch(Port, Token, state.Db);
	}

	public void Relaunch(ushort port, string token) {
		Port = port;
		Token = token;
		Launch();
	}

	public void Stop() {
		ServerLauncher.Stop();
	}

	public void Dispose() {
		Stop();

		if (instance == this) {
			instance = null;
		}
	}
}
