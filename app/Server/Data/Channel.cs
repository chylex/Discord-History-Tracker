namespace DHT.Server.Data {
	public readonly struct Channel {
		public ulong Id { get; internal init; }
		public ulong Server { get; internal init; }
		public string Name { get; internal init; }
		public ulong? ParentId { get; internal init; }
		public int? Position { get; internal init; }
		public string? Topic { get; internal init; }
		public bool? Nsfw { get; internal init; }
	}
}
