namespace DHT.Server.Data;

public readonly struct Attachment {
	public ulong Id { get; internal init; }
	public string Name { get; internal init; }
	public string? Type { get; internal init; }
	public string Url { get; internal init; }
	public ulong Size { get; internal init; }
	public int? Width { get; internal init; }
	public int? Height { get; internal init; }
}
