using System;
using System.Net;
using DHT.Server.Data;

namespace DHT.Server.Download;

public sealed record DownloadItem(string NormalizedUrl, string DownloadUrl, string? Type, ulong? Size) {
	internal Data.Download ToSuccess(long size) {
		return new Data.Download(NormalizedUrl, DownloadUrl, DownloadStatus.Success, Type, (ulong) Math.Max(size, val2: 0));
	}
	
	internal Data.Download ToFailure(HttpStatusCode? statusCode = null) {
		DownloadStatus status = statusCode.HasValue ? (DownloadStatus) (int) statusCode : DownloadStatus.GenericError;
		return new Data.Download(NormalizedUrl, DownloadUrl, status, Type, Size);
	}
}
