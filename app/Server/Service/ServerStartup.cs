using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DHT.Server.Database;
using DHT.Server.Endpoints;
using DHT.Server.Service.Middlewares;
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
		"https://discordapp.com"
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
	public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IDatabaseFile db, ServerParameters parameters) {
		app.UseMiddleware<ServerLoggingMiddleware>();
		app.UseCors();
		app.UseMiddleware<ServerAuthorizationMiddleware>();
		app.UseRouting();
		
		app.UseEndpoints(endpoints => {
			endpoints.MapGet("/get-tracking-script", new GetTrackingScriptEndpoint(db, parameters).Handle);
			endpoints.MapGet("/get-attachment/{url}", new GetAttachmentEndpoint(db).Handle);
			endpoints.MapPost("/track-channel", new TrackChannelEndpoint(db).Handle);
			endpoints.MapPost("/track-users", new TrackUsersEndpoint(db).Handle);
			endpoints.MapPost("/track-messages", new TrackMessagesEndpoint(db).Handle);
		});
	}
}
