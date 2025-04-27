using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Utils.Http;
using DHT.Utils.Logging;
using Sisk.Core.Entity;
using Sisk.Core.Http;

namespace DHT.Server.Endpoints;

abstract class BaseEndpoint {
	private static readonly Log Log = Log.ForType<BaseEndpoint>();
	
	public async Task<HttpResponse> Handle(HttpRequest request) {
		try {
			return await Respond(request);
		} catch (OperationCanceledException) {
			throw;
		} catch (HttpException e) {
			Log.Error(e);
			return new HttpResponse(e.StatusCode).WithContent(e.Message);
		} catch (Exception e) {
			Log.Error(e);
			return new HttpResponse(HttpStatusCode.InternalServerError);
		}
	}
	
	protected abstract Task<HttpResponse> Respond(HttpRequest request);
	
	protected static async Task<JsonElement> ReadJson(HttpRequest request) {
		try {
			return await request.GetJsonContentAsync(JsonElementContext.Default.JsonElement);
		} catch (JsonException) {
			throw new HttpException(HttpStatusCode.UnsupportedMediaType, "This endpoint only accepts JSON.");
		}
	}
	
	protected static Task<HttpResponse> WriteFileIfFound(string relativeFilePath, byte[]? bytes) {
		if (bytes == null) {
			throw new HttpException(HttpStatusCode.NotFound, "File not found: " + relativeFilePath);
		}
		
		HttpResponse response = new HttpResponse(new ByteArrayContent(bytes));
		
		if (MimeTypes.TryGetByFileExtension(relativeFilePath, out var mimeType)) {
			response.Headers.Set(HttpKnownHeaderNames.ContentType, mimeType);
		}
		
		return Task.FromResult(response);
	}
	
	protected static Guid GetSessionId(HttpRequest request) {
		if (request.Query.TryGetValue("session", out StringValue sessionIdValue) && !sessionIdValue.IsNull && Guid.TryParse(sessionIdValue, out Guid sessionId)) {
			return sessionId;
		}
		else {
			throw new HttpException(HttpStatusCode.BadRequest, "Invalid session ID.");
		}
	}
}
