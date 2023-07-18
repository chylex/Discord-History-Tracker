namespace DHT.Server.Database;

/// <summary>
/// A complete snapshot of database statistics at a particular point in time.
/// </summary>
public readonly struct DatabaseStatisticsSnapshot {
	public long TotalServers { get; internal init; }
	public long TotalChannels { get; internal init; }
	public long TotalUsers { get; internal init; }
	public long TotalMessages { get; internal init; }
}
