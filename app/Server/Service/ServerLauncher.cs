using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using DHT.Server.Database;
using DHT.Utils.Logging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace DHT.Server.Service;

public static class ServerLauncher {
	private static readonly Log Log = Log.ForType(typeof(ServerLauncher));

	private static IWebHost? Server { get; set; } = null;

	public static bool IsRunning { get; private set; }
	public static event EventHandler? ServerStatusChanged;
	public static event EventHandler<Exception>? ServerManagementExceptionCaught;

	private static Thread? ManagementThread { get; set; } = null;
	private static readonly Mutex ManagementThreadLock = new();
	private static readonly BlockingCollection<IMessage> Messages = new(new ConcurrentQueue<IMessage>());

	private static void EnqueueMessage(IMessage message) {
		ManagementThreadLock.WaitOne();

		try {
			if (ManagementThread == null) {
				ManagementThread = new Thread(RunManagementThread) {
					Name = "DHT server management thread",
					IsBackground = true
				};
				ManagementThread.Start();
			}

			Messages.Add(message);
		} finally {
			ManagementThreadLock.ReleaseMutex();
		}
	}

	[SuppressMessage("ReSharper", "FunctionNeverReturns")]
	private static void RunManagementThread() {
		foreach (IMessage message in Messages.GetConsumingEnumerable()) {
			try {
				switch (message) {
					case IMessage.StartServer start:
						StopServerFromManagementThread();
						StartServerFromManagementThread(start.Port, start.Token, start.Db);
						break;
					case IMessage.StopServer:
						StopServerFromManagementThread();
						break;
				}
			} catch (Exception e) {
				ServerManagementExceptionCaught?.Invoke(null, e);
			}
		}
	}

	private static void StartServerFromManagementThread(int port, string token, IDatabaseFile db) {
		Log.Info("Starting server on port " + port + "...");

		void AddServices(IServiceCollection services) {
			services.AddSingleton(typeof(IDatabaseFile), db);
			services.AddSingleton(typeof(ServerParameters), new ServerParameters {
				Token = token
			});
		}

		void SetKestrelOptions(KestrelServerOptions options) {
			options.Limits.MaxRequestBodySize = null;
			options.Limits.MinResponseDataRate = null;
			options.ListenLocalhost(port, static listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
		}

		Server = WebHost.CreateDefaultBuilder()
		                .ConfigureServices(AddServices)
		                .UseKestrel(SetKestrelOptions)
		                .UseStartup<Startup>()
		                .Build();

		Server.Start();

		Log.Info("Server started");
		IsRunning = true;
		ServerStatusChanged?.Invoke(null, EventArgs.Empty);
	}

	private static void StopServerFromManagementThread() {
		if (Server != null) {
			Log.Info("Stopping server...");
			Server.StopAsync().Wait();
			Server.Dispose();
			Server = null;

			Log.Info("Server stopped");
			IsRunning = false;
			ServerStatusChanged?.Invoke(null, EventArgs.Empty);
		}
	}

	public static void Relaunch(int port, string token, IDatabaseFile db) {
		EnqueueMessage(new IMessage.StartServer(port, token, db));
	}

	public static void Stop() {
		EnqueueMessage(new IMessage.StopServer());
	}

	private interface IMessage {
		public sealed class StartServer : IMessage {
			public int Port { get; }
			public string Token { get; }
			public IDatabaseFile Db { get; }

			public StartServer(int port, string token, IDatabaseFile db) {
				this.Port = port;
				this.Token = token;
				this.Db = db;
			}
		}

		public sealed class StopServer : IMessage {}
	}
}
