// using System;
// using System.Net;
// using System.Reflection;
// using System.Threading.Tasks;
// using DHT.Utils.Logging;
// using Microsoft.AspNetCore.Http;
// using Microsoft.Extensions.Primitives;
//
// namespace DHT.Server.Service.Middlewares;
//
// sealed class ServerAuthorizationMiddleware(RequestDelegate next, ServerParameters serverParameters) {
// 	private static readonly Log Log = Log.ForType<ServerAuthorizationMiddleware>();
// 	
// 	public async Task InvokeAsync(HttpContext context) {
// 		if (SkipAuthorization(context) || CheckToken(context.Request)) {
// 			await next(context);
// 		}
// 		else {
// 			context.Response.StatusCode = (int) HttpStatusCode.Forbidden;
// 		}
// 	}
// 	
// 	private static bool SkipAuthorization(HttpContext context) {
// 		return context.GetEndpoint()?.RequestDelegate?.Target?.GetType().GetCustomAttribute<NoAuthorization>() != null;
// 	}
// 	
// 	private bool CheckToken(HttpRequest request) {
// 		return HttpMethods.IsGet(request.Method)
// 			       ? CheckToken(request.Query["token"])
// 			       : CheckToken(request.Headers["X-DHT-Token"]);
// 	}
// 	
// 	private bool CheckToken(StringValues token) {
// 		if (token.Count == 1 && token[0] == serverParameters.Token) {
// 			return true;
// 		}
// 		else {
// 			Log.Error("Invalid token: " + (token.Count == 1 ? token[0] : "<missing>"));
// 			return false;
// 		}
// 	}
// 	
// 	[AttributeUsage(AttributeTargets.Class)]
// 	public sealed class NoAuthorization : Attribute;
// }
