namespace DHT.Server.Data.Filters;

public sealed class AttachmentFilter {
	public ulong? MaxBytes { get; set; } = null;

	public DownloadItemRules? DownloadItemRule { get; set; } = null;

	public bool IsEmpty => MaxBytes == null &&
	                       DownloadItemRule == null;

	public enum DownloadItemRules {
		OnlyNotPresent,
		OnlyPresent
	}
}
