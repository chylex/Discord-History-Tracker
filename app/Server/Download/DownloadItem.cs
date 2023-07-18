namespace DHT.Server.Download;

public readonly struct DownloadItem {
	public string Url { get; init; }
	public ulong Size { get; init; }
}
