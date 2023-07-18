using System.Net;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class GetAttachmentEndpoint : BaseEndpoint {
	public GetAttachmentEndpoint(IDatabaseFile db, ServerParameters parameters) : base(db, parameters) {}

	protected override Task<IHttpOutput> Respond(HttpContext ctx) {
		string attachmentUrl = WebUtility.UrlDecode((string) ctx.Request.RouteValues["url"]!);
		DownloadedAttachment? maybeDownloadedAttachment = Db.GetDownloadedAttachment(attachmentUrl);

		if (maybeDownloadedAttachment is {} downloadedAttachment) {
			return Task.FromResult<IHttpOutput>(new HttpOutput.File(downloadedAttachment.Type, downloadedAttachment.Data));
		}
		else {
			return Task.FromResult<IHttpOutput>(new HttpOutput.Redirect(attachmentUrl, permanent: false));
		}
	}
}
