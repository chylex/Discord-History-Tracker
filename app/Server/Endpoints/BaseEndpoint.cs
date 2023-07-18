using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Http;
using DHT.Utils.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;

namespace DHT.Server.Endpoints;

abstract class BaseEndpoint {
	private static readonly Log Log = Log.ForType<BaseEndpoint>();

	protected IDatabaseFile Db { get; }
	private readonly ServerParameters parameters;

	protected BaseEndpoint(IDatabaseFile db, ServerParameters parameters) {
		this.Db = db;
		this.parameters = parameters;
	}

	private async Task Handle(HttpContext ctx, StringValues token) {
		var request = ctx.Request;
		var response = ctx.Response;

		Log.Info("Request: " + request.GetDisplayUrl() + " (" + request.ContentLength + " B)");

		if (token.Count != 1 || token[0] != parameters.Token) {
			Log.Error("Token: " + (token.Count == 1 ? token[0] : "<missing>"));
			response.StatusCode = (int) HttpStatusCode.Forbidden;
			return;
		}

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

	public async Task HandleGet(HttpContext ctx) {
		await Handle(ctx, ctx.Request.Query["token"]);
	}

	public async Task HandlePost(HttpContext ctx) {
		await Handle(ctx, ctx.Request.Headers["X-DHT-Token"]);
	}

	protected abstract Task<IHttpOutput> Respond(HttpContext ctx);

	protected static async Task<JsonElement> ReadJson(HttpContext ctx) {
		return await ctx.Request.ReadFromJsonAsync<JsonElement?>() ?? throw new HttpException(HttpStatusCode.UnsupportedMediaType, "This endpoint only accepts JSON.");
	}
}
