using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DHT.Server.Database;
using DHT.Server.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace DHT.Server.Service;

sealed class ServerStartup {
	private static readonly string[] AllowedOrigins = {
		"https://discord.com",
		"https://ptb.discord.com",
		"https://canary.discord.com",
		"https://discordapp.com",
	};

	public void ConfigureServices(IServiceCollection services) {
		services.AddRoutingCore();

		services.Configure<JsonOptions>(static options => {
			options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
		});

		services.AddCors(static cors => {
			cors.AddDefaultPolicy(static builder => {
				builder.WithOrigins(AllowedOrigins).AllowCredentials().AllowAnyMethod().AllowAnyHeader();
			});
		});
		
		services.AddSingleton<TrackChannelEndpoint>();
		services.AddSingleton<TrackMessagesEndpoint>();
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IDatabaseFile db, ServerAccessToken accessToken) {
		app.UseRouting();
		app.UseCors();
		app.UseEndpoints(static endpoints => {
			endpoints.MapPost("/track-channel", static async (HttpContext context, [FromServices] TrackChannelEndpoint endpoint) => await endpoint.HandlePost(context));
			endpoints.MapPost("/track-messages", static async (HttpContext context, [FromServices] TrackMessagesEndpoint endpoint) => await endpoint.HandlePost(context));
		});
	}
}
