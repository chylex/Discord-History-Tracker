namespace DHT.Server.Data;

public readonly struct Channel {
	public ulong Id { get; init; }
	public ulong Server { get; init; }
	public string Name { get; init; }
	public ulong? ParentId { get; init; }
	public int? Position { get; init; }
	public string? Topic { get; init; }
	public bool? Nsfw { get; init; }
}
