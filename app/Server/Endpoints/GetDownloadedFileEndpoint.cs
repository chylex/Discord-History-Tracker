using System.Net;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class GetDownloadedFileEndpoint : BaseEndpoint {
	public GetDownloadedFileEndpoint(IDatabaseFile db) : base(db) {}

	protected override async Task<IHttpOutput> Respond(HttpContext ctx) {
		string normalizedUrl = WebUtility.UrlDecode((string) ctx.Request.RouteValues["url"]!);
		DownloadWithData? maybeDownloadWithData = await Db.Downloads.GetSuccessfulDownloadWithData(normalizedUrl);

		if (maybeDownloadWithData is { Download: {} download, Data: {} data }) {
			return new HttpOutput.File(download.Type, data);
		}
		else {
			return new HttpOutput.Redirect(normalizedUrl, permanent: false);
		}
	}
}
