namespace DHT.Server.Data {
	public readonly struct Server {
		public ulong Id { get; internal init; }
		public string Name { get; internal init; }
		public ServerType? Type { get; internal init; }
	}
}
