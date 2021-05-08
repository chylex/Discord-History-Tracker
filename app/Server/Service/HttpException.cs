using System;
using System.Net;

namespace DHT.Server.Service {
	public class HttpException : Exception {
		public HttpStatusCode StatusCode { get; }

		public HttpException(HttpStatusCode statusCode, string message) : base(message) {
			StatusCode = statusCode;
		}
	}
}
