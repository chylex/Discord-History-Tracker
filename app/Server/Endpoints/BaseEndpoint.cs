using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Logging;
using DHT.Server.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace DHT.Server.Endpoints {
	public abstract class BaseEndpoint {
		protected IDatabaseFile Db { get; }
		private readonly ServerParameters parameters;

		protected BaseEndpoint(IDatabaseFile db, ServerParameters parameters) {
			this.Db = db;
			this.parameters = parameters;
		}

		public async Task Handle(HttpContext ctx) {
			var request = ctx.Request;
			var response = ctx.Response;

			Log.Info("Request: " + request.GetDisplayUrl() + " (" + request.ContentLength + " B)");

			var requestToken = request.Headers["X-DHT-Token"];
			if (requestToken.Count != 1 || requestToken[0] != parameters.Token) {
				Log.Error("Token: " + (requestToken.Count == 1 ? requestToken[0] : "<missing>"));
				response.StatusCode = (int) HttpStatusCode.Forbidden;
				return;
			}

			try {
				var (statusCode, output) = await Respond(ctx);
				response.StatusCode = (int) statusCode;

				if (output != null) {
					await response.WriteAsJsonAsync(output);
				}
			} catch (HttpException e) {
				Log.Error(e);
				response.StatusCode = (int) e.StatusCode;
				await response.WriteAsync(e.Message);
			} catch (Exception e) {
				Log.Error(e);
				response.StatusCode = (int) HttpStatusCode.InternalServerError;
			}
		}

		protected abstract Task<(HttpStatusCode, object?)> Respond(HttpContext ctx);

		protected static async Task<JsonElement> ReadJson(HttpContext ctx) {
			return await ctx.Request.ReadFromJsonAsync<JsonElement?>() ?? throw new HttpException(HttpStatusCode.UnsupportedMediaType, "This endpoint only accepts JSON.");
		}
	}
}
