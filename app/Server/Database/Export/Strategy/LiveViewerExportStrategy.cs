using System.Net;
using DHT.Server.Data;

namespace DHT.Server.Database.Export.Strategy;

public sealed class LiveViewerExportStrategy : IViewerExportStrategy {
	private readonly string safePort;
	private readonly string safeToken;

	public LiveViewerExportStrategy(ushort port, string token) {
		this.safePort = port.ToString();
		this.safeToken = WebUtility.UrlEncode(token);
	}

	public bool IncludeMessageText => false;

	public string ProcessViewerTemplate(string template) {
		return template.Replace("/*[SERVER_URL]*/", "http://127.0.0.1:" + safePort)
		               .Replace("/*[SERVER_TOKEN]*/", WebUtility.UrlEncode(safeToken));
	}

	public string GetAttachmentUrl(Attachment attachment) {
		return "http://127.0.0.1:" + safePort + "/get-attachment/" + WebUtility.UrlEncode(attachment.NormalizedUrl) + "?token=" + safeToken;
	}
}
