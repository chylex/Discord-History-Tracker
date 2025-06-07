namespace DHT.Server.Data;

public readonly record struct FileUrl(string NormalizedUrl, string DownloadUrl, string? Type) {
	public FileUrl(string url, string? type) : this(url, url, type) {}
	
	public Download ToPendingDownload() {
		return new Download(NormalizedUrl, DownloadUrl, DownloadStatus.Pending, Type, size: null);
	}
}
