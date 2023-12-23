using System.Net;
using System.Threading.Tasks;
using DHT.Utils.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace DHT.Server.Service.Middlewares;

sealed class ServerAuthorizationMiddleware {
	private static readonly Log Log = Log.ForType<ServerAuthorizationMiddleware>();

	private readonly RequestDelegate next;
	private readonly ServerParameters serverParameters;

	public ServerAuthorizationMiddleware(RequestDelegate next, ServerParameters serverParameters) {
		this.next = next;
		this.serverParameters = serverParameters;
	}

	public async Task InvokeAsync(HttpContext context) {
		var request = context.Request;

		bool success = HttpMethods.IsGet(request.Method)
			               ? CheckToken(request.Query["token"])
			               : CheckToken(request.Headers["X-DHT-Token"]);

		if (success) {
			await next(context);
		}
		else {
			context.Response.StatusCode = (int) HttpStatusCode.Forbidden;
		}
	}

	private bool CheckToken(StringValues token) {
		if (token.Count == 1 && token[0] == serverParameters.Token) {
			return true;
		}
		else {
			Log.Error("Invalid token: " + (token.Count == 1 ? token[0] : "<missing>"));
			return false;
		}
	}
}
