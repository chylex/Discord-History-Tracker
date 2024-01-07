using System.Net;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Download;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class GetDownloadedFileEndpoint : BaseEndpoint {
	public GetDownloadedFileEndpoint(IDatabaseFile db) : base(db) {}

	protected override async Task<IHttpOutput> Respond(HttpContext ctx) {
		string url = WebUtility.UrlDecode((string) ctx.Request.RouteValues["url"]!);
		string normalizedUrl = DiscordCdn.NormalizeUrl(url);
		
		if (await Db.Downloads.GetSuccessfulDownloadWithData(normalizedUrl) is { Download: {} download, Data: {} data }) {
			return new HttpOutput.File(download.Type, data);
		}
		else {
			return new HttpOutput.Redirect(url, permanent: false);
		}
	}
}
