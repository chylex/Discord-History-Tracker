namespace DHT.Server.Data;

public readonly struct User {
	public ulong Id { get; init; }
	public string Name { get; init; }
	public string? AvatarUrl { get; init; }
	public string? Discriminator { get; init; }
}
