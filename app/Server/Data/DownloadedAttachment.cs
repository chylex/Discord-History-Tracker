namespace DHT.Server.Data;

public readonly struct DownloadedAttachment {
	public string? Type { get; internal init; }
	public byte[] Data { get; internal init; }
}
