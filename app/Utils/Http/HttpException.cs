using System;
using System.Net;

namespace DHT.Utils.Http;

public sealed class HttpException : Exception {
	public HttpStatusCode StatusCode { get; }

	public HttpException(HttpStatusCode statusCode, string message) : base(message) {
		StatusCode = statusCode;
	}
}
