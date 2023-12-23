using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Utils.Http;
using DHT.Utils.Logging;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

abstract class BaseEndpoint {
	private static readonly Log Log = Log.ForType<BaseEndpoint>();

	protected IDatabaseFile Db { get; }

	protected BaseEndpoint(IDatabaseFile db) {
		this.Db = db;
	}

	public async Task Handle(HttpContext ctx) {
		var response = ctx.Response;

		try {
			response.StatusCode = (int) HttpStatusCode.OK;
			var output = await Respond(ctx);
			await output.WriteTo(response);
		} catch (HttpException e) {
			Log.Error(e);
			response.StatusCode = (int) e.StatusCode;
			await response.WriteAsync(e.Message);
		} catch (Exception e) {
			Log.Error(e);
			response.StatusCode = (int) HttpStatusCode.InternalServerError;
		}
	}

	protected abstract Task<IHttpOutput> Respond(HttpContext ctx);

	protected static async Task<JsonElement> ReadJson(HttpContext ctx) {
		try {
			return await ctx.Request.ReadFromJsonAsync(JsonElementContext.Default.JsonElement);
		} catch (JsonException) {
			throw new HttpException(HttpStatusCode.UnsupportedMediaType, "This endpoint only accepts JSON.");
		}
	}
}
