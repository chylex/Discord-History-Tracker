using System.Net;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class GetAttachmentEndpoint : BaseEndpoint {
	public GetAttachmentEndpoint(IDatabaseFile db) : base(db) {}

	protected override async Task<IHttpOutput> Respond(HttpContext ctx) {
		string attachmentUrl = WebUtility.UrlDecode((string) ctx.Request.RouteValues["url"]!);
		DownloadedAttachment? maybeDownloadedAttachment = await Db.Downloads.GetDownloadedAttachment(attachmentUrl);

		if (maybeDownloadedAttachment is {} downloadedAttachment) {
			return new HttpOutput.File(downloadedAttachment.Type, downloadedAttachment.Data);
		}
		else {
			return new HttpOutput.Redirect(attachmentUrl, permanent: false);
		}
	}
}
