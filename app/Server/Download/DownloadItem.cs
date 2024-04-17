using System;
using System.Net;
using DHT.Server.Data;

namespace DHT.Server.Download;

public readonly struct DownloadItem {
	public string NormalizedUrl { get; init; }
	public string DownloadUrl { get; init; }
	public string? Type { get; init; }
	public ulong? Size { get; init; }
	
	internal Data.Download ToSuccess(long size) {
		return new Data.Download(NormalizedUrl, DownloadUrl, DownloadStatus.Success, Type, (ulong) Math.Max(size, 0));
	}
	
	internal Data.Download ToFailure(HttpStatusCode? statusCode = null) {
		var status = statusCode.HasValue ? (DownloadStatus) (int) statusCode : DownloadStatus.GenericError;
		return new Data.Download(NormalizedUrl, DownloadUrl, status, Type, Size);
	}
}
