using DHT.Server.Data;

namespace DHT.Server.Database.Export.Strategy;

public sealed class StandaloneViewerExportStrategy : IViewerExportStrategy {
	public static StandaloneViewerExportStrategy Instance { get; } = new ();

	private StandaloneViewerExportStrategy() {}

	public string GetAttachmentUrl(Attachment attachment) {
		// The normalized URL will not load files from Discord CDN once the time limit is enforced.
		
		// The downloaded URL would work, but only for a limited time, so it is better for the links to not work
		// rather than give users a false sense of security.
		
		return attachment.NormalizedUrl;
	}
}
