using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Endpoints;
using DHT.Server.Service.Viewer;
using DHT.Utils.Logging;
using DHT.Utils.Resources;
using Sisk.Core.Entity;
using Sisk.Core.Http;
using Sisk.Core.Http.Hosting;
using Router = Sisk.Core.Routing.Router;

namespace DHT.Server.Service;

public sealed class ServerManager {
	private static readonly Log Log = Log.ForType(typeof(ServerManager));
	
	private HttpServerHostContext? server;
	public bool IsRunning => server != null;
	
	public event EventHandler<Status>? StatusChanged;
	
	public enum Status {
		Starting,
		Started,
		Stopping,
		Stopped,
	}
	
	private readonly IDatabaseFile db;
	private readonly ViewerSessions viewerSessions;
	private readonly SemaphoreSlim semaphore = new (initialCount: 1, maxCount: 1);
	
	internal ServerManager(IDatabaseFile db, ViewerSessions viewerSessions) {
		this.db = db;
		this.viewerSessions = viewerSessions;
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
			StopInternal();
		} finally {
			semaphore.Release();
		}
	}
	
	private async Task StartInternal(ushort port, string token) {
		StopInternal();
		
		StatusChanged?.Invoke(this, Status.Starting);
		
		ServerParameters parameters = new ServerParameters(port, token);
		ResourceLoader resources = new ResourceLoader(Assembly.GetExecutingAssembly());
		
		static void ConfigureServer(HttpServerConfiguration config) {
			config.AsyncRequestProcessing = true;
			config.IncludeRequestIdHeader = false;
			config.MaximumContentLength = 0;
			config.SendSiskHeader = false;
			config.ThrowExceptions = false;
		}
		
		void ConfigureRoutes(Router router) {
			router.CallbackErrorHandler = static (exception, _) => {
				Log.Error(exception);
				return new HttpResponse(HttpStatusCode.InternalServerError).WithContent("An error occurred.");
			};
			
			router.MapGet("/get-downloaded-file/<url>", new GetDownloadedFileEndpoint(db).Handle);
			router.MapGet("/get-tracking-script", new GetTrackingScriptEndpoint(parameters, resources).Handle);
			router.MapGet("/get-userscript/<ignored>", new GetUserscriptEndpoint(resources).Handle);
			router.MapGet("/get-viewer-messages", new GetViewerMessagesEndpoint(db, viewerSessions).Handle);
			router.MapGet("/get-viewer-metadata", new GetViewerMetadataEndpoint(db, viewerSessions).Handle);
			router.MapGet("/viewer/<<path>>", new ViewerEndpoint(resources).Handle);
			
			router.MapPost("/track-channel", new TrackChannelEndpoint(db).Handle);
			router.MapPost("/track-messages", new TrackMessagesEndpoint(db).Handle);
			router.MapPost("/track-users", new TrackUsersEndpoint(db).Handle);
		}
		
		string[] allowedOrigins = [
			"https://discord.com",
			"https://ptb.discord.com",
			"https://canary.discord.com",
			"https://discordapp.com",
		];
		
		HttpServerHostContext newServer = HttpServer.CreateBuilder()
		                                            .UseListeningPort(new ListeningPort(secure: false, "127.0.0.1", port))
		                                            .UseCors(new CrossOriginResourceSharingHeaders(allowOrigins: allowedOrigins, exposeHeaders: [ "X-DHT" ]))
		                                            .UseConfiguration(ConfigureServer)
		                                            .UseRouter(ConfigureRoutes)
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
	
	private void StopInternal() {
		if (server == null) {
			return;
		}
		
		StatusChanged?.Invoke(this, Status.Stopping);
		
		Log.Info("Stopping server...");
		server.Dispose();
		Log.Info("Server stopped");
		
		server.Dispose();
		server = null;
		
		StatusChanged?.Invoke(this, Status.Stopped);
	}
}
