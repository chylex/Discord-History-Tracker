using DHT.Server.Download;

namespace DHT.Server.Data;

public readonly struct Server {
	public ulong Id { get; init; }
	public string Name { get; init; }
	public ServerType? Type { get; init; }
	public string? IconHash { get; init; }
	
	internal FileUrl? IconUrl => Type == null || IconHash == null ? null : DownloadLinkExtractor.ServerIcon(Type.Value, Id, IconHash);
}
