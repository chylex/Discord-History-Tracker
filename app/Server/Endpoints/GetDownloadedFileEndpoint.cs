using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Download;
using Sisk.Core.Http;
using Sisk.Core.Http.Streams;

namespace DHT.Server.Endpoints;

sealed class GetDownloadedFileEndpoint(IDatabaseFile db) : BaseEndpoint {
	protected override async Task<HttpResponse> Respond(HttpRequest request) {
		string url = WebUtility.UrlDecode(request.RouteParameters.GetItem("url"));
		string normalizedUrl = DiscordCdn.NormalizeUrl(url);
		
		HttpResponseStreamManager response = request.GetResponseStream();
		
		if (!await db.Downloads.GetSuccessfulDownloadWithData(normalizedUrl, WriteDataTo(response), CancellationToken.None)) {
			response.SetStatus(HttpStatusCode.Redirect);
			response.SetHeader(HttpKnownHeaderNames.Location, url);
		}
		
		return response.Close();
	}
	
	private static Func<Data.Download, Stream, CancellationToken, Task> WriteDataTo(HttpResponseStreamManager response) {
		return (download, stream, cancellationToken) => {
			response.SetStatus(HttpStatusCode.OK);
			response.SetHeader(HttpKnownHeaderNames.ContentType, download.Type);
			
			if (download.Size is {} size) {
				response.SetContentLength((long) size);
			}
			else {
				response.SendChunked = true;
			}
			
			return stream.CopyToAsync(response.ResponseStream, cancellationToken);
		};
	}
}
