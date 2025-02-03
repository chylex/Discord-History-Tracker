namespace DHT.Server.Data.Aggregations;

public sealed class DownloadStatusStatistics {
	public int PendingCount { get; internal init; }
	public ulong PendingTotalSize { get; internal init; }
	public int PendingWithUnknownSizeCount { get; internal init; }
	
	public int SuccessfulCount { get; internal init; }
	public ulong SuccessfulTotalSize { get; internal init; }
	public int SuccessfulWithUnknownSizeCount { get; internal init; }
	
	public int FailedCount { get; internal init; }
	public ulong FailedTotalSize { get; internal init; }
	public int FailedWithUnknownSizeCount { get; internal init; }
	
	public int SkippedCount { get; internal init; }
	public ulong SkippedTotalSize { get; internal init; }
	public int SkippedWithUnknownSizeCount { get; internal init; }
}
