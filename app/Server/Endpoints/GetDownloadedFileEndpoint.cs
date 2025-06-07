using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Download;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class GetDownloadedFileEndpoint(IDatabaseFile db) : BaseEndpoint {
	protected override async Task Respond(HttpRequest request, HttpResponse response, CancellationToken cancellationToken) {
		string url = WebUtility.UrlDecode((string) request.RouteValues["url"]!);
		string normalizedUrl = DiscordCdn.NormalizeUrl(url);
		
		if (!await db.Downloads.GetSuccessfulDownloadWithData(normalizedUrl, WriteDataTo(response), cancellationToken)) {
			response.Redirect(url, permanent: false);
		}
	}
	
	private static Func<Data.Download, Stream, CancellationToken, Task> WriteDataTo(HttpResponse response) {
		return (download, stream, cancellationToken) => response.WriteStreamAsync(download.Type, download.Size, stream, cancellationToken);
	}
}
