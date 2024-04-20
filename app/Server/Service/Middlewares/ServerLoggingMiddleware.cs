using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DHT.Utils.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace DHT.Server.Service.Middlewares;

sealed class ServerLoggingMiddleware(RequestDelegate next) {
	private static readonly Log Log = Log.ForType<ServerLoggingMiddleware>();

	public async Task InvokeAsync(HttpContext context) {
		var stopwatch = Stopwatch.StartNew();
		try {
			await next(context);
		} catch (OperationCanceledException) {
			OnFinished(stopwatch, context);
			throw;
		}

		OnFinished(stopwatch, context);
	}

	private static void OnFinished(Stopwatch stopwatch, HttpContext context) {
		stopwatch.Stop();

		var request = context.Request;
		var requestLength = request.ContentLength ?? 0L;
		var elapsedMs = stopwatch.ElapsedMilliseconds;

		if (context.RequestAborted.IsCancellationRequested) {
			Log.Debug("Request to " + request.GetEncodedPathAndQuery() + " (" + requestLength + " B) was cancelled after " + elapsedMs + " ms");
		}
		else {
			var responseStatus = context.Response.StatusCode;
			Log.Debug("Request to " + request.GetEncodedPathAndQuery() + " (" + requestLength + " B) returned " + responseStatus + ", took " + elapsedMs + " ms");
		}
	}
}
