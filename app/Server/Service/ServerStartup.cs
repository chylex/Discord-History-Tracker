using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DHT.Server.Database;
using DHT.Server.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DHT.Server.Service {
	sealed class Startup {
		public void ConfigureServices(IServiceCollection services) {
			services.Configure<JsonOptions>(static options => {
				options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
			});

			services.AddCors(static cors => {
				cors.AddDefaultPolicy(static builder => {
					builder.WithOrigins("https://discord.com", "https://discordapp.com").AllowCredentials().AllowAnyMethod().AllowAnyHeader();
				});
			});
		}

		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IDatabaseFile db, ServerParameters parameters) {
			app.UseRouting();
			app.UseCors();
			app.UseEndpoints(endpoints => {
				TrackChannelEndpoint trackChannel = new(db, parameters);
				endpoints.MapPost("/track-channel", async context => await trackChannel.Handle(context));

				TrackUsersEndpoint trackUsers = new(db, parameters);
				endpoints.MapPost("/track-users", async context => await trackUsers.Handle(context));

				TrackMessagesEndpoint trackMessages = new(db, parameters);
				endpoints.MapPost("/track-messages", async context => await trackMessages.Handle(context));
			});
		}
	}
}
