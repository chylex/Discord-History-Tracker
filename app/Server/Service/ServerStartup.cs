using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DHT.Server.Database;
using DHT.Server.Endpoints;
using DHT.Server.Service.Middlewares;
using DHT.Server.Service.Viewer;
using DHT.Utils.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DHT.Server.Service;

sealed class Startup {
	private static readonly string[] AllowedOrigins = [
		"https://discord.com",
		"https://ptb.discord.com",
		"https://canary.discord.com",
		"https://discordapp.com",
	];
	
	public void ConfigureServices(IServiceCollection services) {
		services.Configure<JsonOptions>(static options => {
			options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
		});
		
		services.AddCors(static cors => {
			cors.AddDefaultPolicy(static builder => {
				builder.WithOrigins(AllowedOrigins).AllowCredentials().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("X-DHT");
			});
		});
		
		services.AddRoutingCore();
	}
	
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IDatabaseFile db, ServerParameters parameters, ResourceLoader resources, ViewerSessions viewerSessions) {
		app.UseMiddleware<ServerLoggingMiddleware>();
		app.UseCors();
		app.UseRouting();
		app.UseMiddleware<ServerAuthorizationMiddleware>();
		app.UseEndpoints(endpoints => {
			endpoints.MapGet("/get-downloaded-file/{url}", new GetDownloadedFileEndpoint(db).Handle);
			endpoints.MapGet("/get-tracking-script", new GetTrackingScriptEndpoint(parameters, resources).Handle);
			endpoints.MapGet("/get-userscript/{**ignored}", new GetUserscriptEndpoint(resources).Handle);
			endpoints.MapGet("/get-viewer-messages", new GetViewerMessagesEndpoint(db, viewerSessions).Handle);
			endpoints.MapGet("/get-viewer-metadata", new GetViewerMetadataEndpoint(db, viewerSessions).Handle);
			endpoints.MapGet("/viewer/{**path}", new ViewerEndpoint(resources).Handle);
			
			endpoints.MapPost("/track-channel", new TrackChannelEndpoint(db).Handle);
			endpoints.MapPost("/track-messages", new TrackMessagesEndpoint(db).Handle);
			endpoints.MapPost("/track-users", new TrackUsersEndpoint(db).Handle);
		});
	}
}
