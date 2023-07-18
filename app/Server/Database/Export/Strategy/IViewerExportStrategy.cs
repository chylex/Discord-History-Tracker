using DHT.Server.Data;

namespace DHT.Server.Database.Export.Strategy;

public interface IViewerExportStrategy {
	string GetAttachmentUrl(Attachment attachment);
}
