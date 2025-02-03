namespace DHT.Server.Data;

public sealed class Download {
	public string NormalizedUrl { get; }
	public string DownloadUrl { get; }
	public DownloadStatus Status { get; }
	public string? Type { get; }
	public ulong? Size { get; }
	
	internal Download(string normalizedUrl, string downloadUrl, DownloadStatus status, string? type, ulong? size) {
		NormalizedUrl = normalizedUrl;
		DownloadUrl = downloadUrl;
		Status = status;
		Type = type;
		Size = size;
	}
}
