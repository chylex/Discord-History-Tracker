using System.Net;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Download;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class GetDownloadedFileEndpoint(IDatabaseFile db) : BaseEndpoint(db) {
	protected override async Task Respond(HttpRequest request, HttpResponse response) {
		string url = WebUtility.UrlDecode((string) request.RouteValues["url"]!);
		string normalizedUrl = DiscordCdn.NormalizeUrl(url);
		
		if (!await Db.Downloads.GetSuccessfulDownloadWithData(normalizedUrl, (download, stream) => response.WriteStreamAsync(download.Type, download.Size, stream))) {
			response.Redirect(url, permanent: false);
		}
	}
}
