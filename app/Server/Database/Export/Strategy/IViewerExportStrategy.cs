using DHT.Server.Data;

namespace DHT.Server.Database.Export.Strategy;

public interface IViewerExportStrategy {
	bool IncludeMessageText { get; }
	string ProcessViewerTemplate(string template);
	string GetAttachmentUrl(Attachment attachment);
}
