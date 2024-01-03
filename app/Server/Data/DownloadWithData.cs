namespace DHT.Server.Data;

public readonly record struct DownloadWithData(Download Download, byte[]? Data);
