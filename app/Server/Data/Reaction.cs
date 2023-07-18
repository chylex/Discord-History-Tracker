namespace DHT.Server.Data;

public readonly struct Reaction {
	public ulong? EmojiId { get; internal init; }
	public string? EmojiName { get; internal init; }
	public EmojiFlags EmojiFlags { get; internal init; }
	public int Count { get; internal init; }
}
