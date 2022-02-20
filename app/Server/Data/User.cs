namespace DHT.Server.Data {
	public readonly struct User {
		public ulong Id { get; internal init; }
		public string Name { get; internal init; }
		public string? AvatarUrl { get; internal init; }
		public string? Discriminator { get; internal init; }
	}
}
