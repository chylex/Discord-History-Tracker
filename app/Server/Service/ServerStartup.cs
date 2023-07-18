using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DHT.Server.Database;
using DHT.Server.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DHT.Server.Service;

sealed class Startup {
	private static readonly string[] AllowedOrigins = {
		"https://discord.com",
		"https://ptb.discord.com",
		"https://canary.discord.com",
		"https://discordapp.com",
	};

	public void ConfigureServices(IServiceCollection services) {
		services.Configure<JsonOptions>(static options => {
			options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
		});

		services.AddCors(static cors => {
			cors.AddDefaultPolicy(static builder => {
				builder.WithOrigins(AllowedOrigins).AllowCredentials().AllowAnyMethod().AllowAnyHeader();
			});
		});
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IDatabaseFile db, ServerParameters parameters) {
		app.UseRouting();
		app.UseCors();
		app.UseEndpoints(endpoints => {
			TrackChannelEndpoint trackChannel = new(db, parameters);
			endpoints.MapPost("/track-channel", async context => await trackChannel.HandlePost(context));

			TrackUsersEndpoint trackUsers = new(db, parameters);
			endpoints.MapPost("/track-users", async context => await trackUsers.HandlePost(context));

			TrackMessagesEndpoint trackMessages = new(db, parameters);
			endpoints.MapPost("/track-messages", async context => await trackMessages.HandlePost(context));

			GetAttachmentEndpoint getAttachment = new(db, parameters);
			endpoints.MapGet("/get-attachment/{url}", async context => await getAttachment.HandleGet(context));
		});
	}
}
