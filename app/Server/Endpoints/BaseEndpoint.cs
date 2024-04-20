using System;
using System.Net;
using System.Text.Json;
using System.Threading;
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
			await Respond(ctx.Request, response, ctx.RequestAborted);
		} catch (OperationCanceledException) {
			throw;
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

	protected abstract Task Respond(HttpRequest request, HttpResponse response, CancellationToken cancellationToken);

	protected static async Task<JsonElement> ReadJson(HttpRequest request) {
		try {
			return await request.ReadFromJsonAsync(JsonElementContext.Default.JsonElement);
		} catch (JsonException) {
			throw new HttpException(HttpStatusCode.UnsupportedMediaType, "This endpoint only accepts JSON.");
		}
	}
	
	protected static Guid GetSessionId(HttpRequest request) {
		if (request.Query.TryGetValue("session", out var sessionIdValue) && sessionIdValue.Count == 1 && Guid.TryParse(sessionIdValue[0], out Guid sessionId)) {
			return sessionId;
		}
		else {
			throw new HttpException(HttpStatusCode.BadRequest, "Invalid session ID.");
		}
	}
}
