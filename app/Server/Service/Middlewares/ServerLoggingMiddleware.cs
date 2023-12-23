using System.Diagnostics;
using System.Threading.Tasks;
using DHT.Utils.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace DHT.Server.Service.Middlewares; 

sealed class ServerLoggingMiddleware {
	private static readonly Log Log = Log.ForType<ServerLoggingMiddleware>();
	
	private readonly RequestDelegate next;
	
	public ServerLoggingMiddleware(RequestDelegate next) {
		this.next = next;
	}

	public async Task InvokeAsync(HttpContext context) {
		var stopwatch = Stopwatch.StartNew();
		await next(context);
		stopwatch.Stop();
		
		var request = context.Request;
		var requestLength = request.ContentLength ?? 0L;
		var responseStatus = context.Response.StatusCode;
		var elapsedMs = stopwatch.ElapsedMilliseconds;
		Log.Debug("Request to " + request.GetEncodedPathAndQuery() + " (" + requestLength + " B) returned " + responseStatus + ", took " + elapsedMs + " ms");
	}
}
