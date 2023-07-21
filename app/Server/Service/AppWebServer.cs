using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Utils.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace DHT.Server.Service;

sealed class AppWebServer {
	private static readonly Log Log = Log.ForType<AppWebServer>();

	private readonly IDatabaseFile db;
	private readonly SemaphoreSlim serverManagementSemaphore;

	private IWebHost? server;

	public bool IsRunning { get; private set; }

	public AppWebServer(IDatabaseFile db) {
		this.db = db;
		this.serverManagementSemaphore = new SemaphoreSlim(1, 1);
	}

	public async Task Start(int port, string token) {
		await AcquireServerManagementSemaphore();
		try {
			await StartImpl(port, token);
			IsRunning = true;
		} finally {
			serverManagementSemaphore.Release();
		}
	}

	public async Task Stop() {
		await AcquireServerManagementSemaphore();
		try {
			await StopImpl();
			IsRunning = false;
		} finally {
			serverManagementSemaphore.Release();
		}
	}

	private async Task StartImpl(int port, string token) {
		if (server != null) {
			await StopImpl();
		}

		Log.Info("Starting server on port " + port + "...");

		void AddServices(IServiceCollection services) {
			services.AddSingleton(typeof(IDatabaseFile), db);
			services.AddSingleton(new ServerAccessToken(token));
		}

		void SetKestrelOptions(KestrelServerOptions options) {
			options.Limits.MaxRequestBodySize = null;
			options.Limits.MinResponseDataRate = null;
			options.ListenLocalhost(port, static listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
		}

		server = new WebHostBuilder()
		         .ConfigureServices(AddServices)
		         .UseKestrelCore()
		         .ConfigureKestrel(SetKestrelOptions)
		         .UseStartup<ServerStartup>()
		         .Build();

		await server.StartAsync();
		Log.Info("Server started.");
	}

	private async Task StopImpl() {
		if (server == null) {
			return;
		}

		Log.Info("Stopping server...");

		await server.StopAsync();
		server.Dispose();

		Log.Info("Server stopped.");
		server = null;
	}

	private async Task AcquireServerManagementSemaphore() {
		if (!await serverManagementSemaphore.WaitAsync(0)) {
			Log.Info("Waiting for previous action to finish...");
			await serverManagementSemaphore.WaitAsync();
		}
	}
}
