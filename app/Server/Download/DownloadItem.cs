namespace DHT.Server.Download;

public readonly struct DownloadItem {
	public string NormalizedUrl { get; init; }
	public string DownloadUrl { get; init; }
	public ulong Size { get; init; }
}
