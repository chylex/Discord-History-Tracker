namespace DHT.Server.Database.Export;

readonly record struct Snowflake(ulong Id) {
	public static implicit operator Snowflake(ulong id) => new (id);
}
