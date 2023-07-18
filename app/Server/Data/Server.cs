namespace DHT.Server.Data;

public readonly struct Server {
	public ulong Id { get; init; }
	public string Name { get; init; }
	public ServerType? Type { get; init; }
}
