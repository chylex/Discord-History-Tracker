using System;
using System.Net;
using DHT.Server.Data;

namespace DHT.Server.Download;

public readonly struct DownloadItem {
	public string NormalizedUrl { get; init; }
	public string DownloadUrl { get; init; }
	public string? Type { get; init; }
	public ulong? Size { get; init; }
	
	internal DownloadWithData ToSuccess(byte[] data) {
		var size = (ulong) Math.Max(data.LongLength, 0);
		return new DownloadWithData(new Data.Download(NormalizedUrl, DownloadUrl, DownloadStatus.Success, Type, size), data);
	}
	
	internal DownloadWithData ToFailure(HttpStatusCode? statusCode = null) {
		var status = statusCode.HasValue ? (DownloadStatus) (int) statusCode : DownloadStatus.GenericError;
		return new DownloadWithData(new Data.Download(NormalizedUrl, DownloadUrl, status, Type, Size), Data: null);
	}
}
