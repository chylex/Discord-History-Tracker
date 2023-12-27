using System;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Utils.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace DHT.Server.Service;

public sealed class ServerManager {
	private static readonly Log Log = Log.ForType(typeof(ServerManager));

	private IWebHost? server;
	public bool IsRunning => server != null;
	
	public event EventHandler<Status>? StatusChanged;

	public enum Status {
		Starting,
		Started,
		Stopping,
		Stopped
	}

	private readonly IDatabaseFile db;
	private readonly SemaphoreSlim semaphore = new (1, 1);

	internal ServerManager(IDatabaseFile db) {
		this.db = db;
	}

	public async Task Start(ushort port, string token) {
		await semaphore.WaitAsync();
		try {
			await StartInternal(port, token);
		} finally {
			semaphore.Release();
		}
	}

	public async Task Stop() {
		await semaphore.WaitAsync();
		try {
			await StopInternal();
		} finally {
			semaphore.Release();
		}
	}

	private async Task StartInternal(ushort port, string token) {
		await StopInternal();
		
		StatusChanged?.Invoke(this, Status.Starting);

		void AddServices(IServiceCollection services) {
			services.AddSingleton(typeof(IDatabaseFile), db);
			services.AddSingleton(typeof(ServerParameters), new ServerParameters(port, token));
		}

		void SetKestrelOptions(KestrelServerOptions options) {
			options.Limits.MaxRequestBodySize = null;
			options.Limits.MinResponseDataRate = null;
			options.ListenLocalhost(port, static listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
		}

		var newServer = new WebHostBuilder()
		                .ConfigureServices(AddServices)
		                .UseKestrel(SetKestrelOptions)
		                .UseStartup<Startup>()
		                .Build();

		Log.Info("Starting server on port " + port + "...");
		
		try {
			await newServer.StartAsync();
		} catch (Exception) {
			Log.Error("Server could not start");
			StatusChanged?.Invoke(this, Status.Stopped);
			throw;
		}

		Log.Info("Server started");

		server = newServer;
		
		StatusChanged?.Invoke(this, Status.Started);
	}

	private async Task StopInternal() {
		if (server == null) {
			return;
		}

		StatusChanged?.Invoke(this, Status.Stopping);
		
		Log.Info("Stopping server...");
		await server.StopAsync();
		Log.Info("Server stopped");

		server.Dispose();
		server = null;
		
		StatusChanged?.Invoke(this, Status.Stopped);
	}
}
