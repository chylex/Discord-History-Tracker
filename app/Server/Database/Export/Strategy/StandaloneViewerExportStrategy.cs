using DHT.Server.Data;

namespace DHT.Server.Database.Export.Strategy;

public sealed class StandaloneViewerExportStrategy : IViewerExportStrategy {
	public static StandaloneViewerExportStrategy Instance { get; } = new ();

	private StandaloneViewerExportStrategy() {}

	public string GetAttachmentUrl(Attachment attachment) {
		return attachment.Url;
	}
}
