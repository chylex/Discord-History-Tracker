using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DHT.Server.Database;
using DHT.Server.Endpoints;
using DHT.Server.Service.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DHT.Server.Service;

sealed class Startup {
	private static readonly string[] AllowedOrigins = {
		"https://discord.com",
		"https://ptb.discord.com",
		"https://canary.discord.com",
		"https://discordapp.com",
	};

	public void ConfigureServices(IServiceCollection services) {
		services.AddLogging(static logging => {
			logging.ClearProviders();
		});
		
		services.Configure<JsonOptions>(static options => {
			options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
		});

		services.AddCors(static cors => {
			cors.AddDefaultPolicy(static builder => {
				builder.WithOrigins(AllowedOrigins).AllowCredentials().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("X-DHT");
			});
		});
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IDatabaseFile db, ServerParameters parameters) {
		app.UseMiddleware<ServerLoggingMiddleware>();
		app.UseRouting();
		app.UseCors();
		app.UseEndpoints(endpoints => {
			GetTrackingScriptEndpoint getTrackingScript = new (db, parameters);
			endpoints.MapGet("/get-tracking-script", context => getTrackingScript.HandleGet(context));
			
			TrackChannelEndpoint trackChannel = new (db, parameters);
			endpoints.MapPost("/track-channel", context => trackChannel.HandlePost(context));

			TrackUsersEndpoint trackUsers = new (db, parameters);
			endpoints.MapPost("/track-users", context => trackUsers.HandlePost(context));

			TrackMessagesEndpoint trackMessages = new (db, parameters);
			endpoints.MapPost("/track-messages", context => trackMessages.HandlePost(context));

			GetAttachmentEndpoint getAttachment = new (db, parameters);
			endpoints.MapGet("/get-attachment/{url}", context => getAttachment.HandleGet(context));
		});
	}
}
