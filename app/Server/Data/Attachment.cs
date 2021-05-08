namespace DHT.Server.Data {
	public readonly struct Attachment {
		public ulong Id { get; init; }
		public string Name { get; init; }
		public string? Type { get; init; }
		public string Url { get; init; }
		public ulong Size { get; init; }
	}
}
