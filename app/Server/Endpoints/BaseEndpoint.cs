using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Utils.Http;
using DHT.Utils.Logging;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

abstract class BaseEndpoint(IDatabaseFile db) {
	private static readonly Log Log = Log.ForType<BaseEndpoint>();

	protected IDatabaseFile Db { get; } = db;

	public async Task Handle(HttpContext ctx) {
		var response = ctx.Response;

		try {
			response.StatusCode = (int) HttpStatusCode.OK;
			await Respond(ctx.Request, response);
		} catch (HttpException e) {
			Log.Error(e);
			response.StatusCode = (int) e.StatusCode;
			if (response.HasStarted) {
				Log.Warn("Response has already started, cannot write status message: " + e.Message);
			}
			else {
				await response.WriteAsync(e.Message);
			}
		} catch (Exception e) {
			Log.Error(e);
			response.StatusCode = (int) HttpStatusCode.InternalServerError;
		}
	}

	protected abstract Task Respond(HttpRequest request, HttpResponse response);

	protected static async Task<JsonElement> ReadJson(HttpRequest request) {
		try {
			return await request.ReadFromJsonAsync(JsonElementContext.Default.JsonElement);
		} catch (JsonException) {
			throw new HttpException(HttpStatusCode.UnsupportedMediaType, "This endpoint only accepts JSON.");
		}
	}
}
