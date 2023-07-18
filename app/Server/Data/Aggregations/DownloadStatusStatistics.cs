namespace DHT.Server.Data.Aggregations;

public sealed class DownloadStatusStatistics {
	public int EnqueuedCount { get; internal set; }
	public ulong EnqueuedSize { get; internal set; }

	public int SuccessfulCount { get; internal set; }
	public ulong SuccessfulSize { get; internal set; }

	public int FailedCount { get; internal set; }
	public ulong FailedSize { get; internal set; }

	public int SkippedCount { get; internal set; }
	public ulong SkippedSize { get; internal set; }
}
