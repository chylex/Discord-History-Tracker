using DHT.Server.Download;

namespace DHT.Server.Data;

public readonly struct User {
	public ulong Id { get; init; }
	public string Name { get; init; }
	public string? DisplayName { get; init; }
	public string? AvatarHash { get; init; }
	public string? Discriminator { get; init; }
	
	internal FileUrl? AvatarUrl => AvatarHash == null ? null : DownloadLinkExtractor.UserAvatar(Id, AvatarHash);
}
