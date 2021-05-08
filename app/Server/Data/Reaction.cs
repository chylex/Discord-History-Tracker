namespace DHT.Server.Data {
	public readonly struct Reaction {
		public ulong? EmojiId { get; init; }
		public string? EmojiName { get; init; }
		public EmojiFlags EmojiFlags { get; init; }
		public int Count { get; init; }
	}
}
