using System;
using System.Net;
using DHT.Server.Download;

namespace DHT.Server.Data;

public readonly struct Download {
	internal static Download NewSuccess(DownloadItem item, byte[] data) {
		return new Download(item.NormalizedUrl, item.DownloadUrl, DownloadStatus.Success, (ulong) Math.Max(data.LongLength, 0), data);
	}

	internal static Download NewFailure(DownloadItem item, HttpStatusCode? statusCode, ulong size) {
		return new Download(item.NormalizedUrl, item.DownloadUrl, statusCode.HasValue ? (DownloadStatus) (int) statusCode : DownloadStatus.GenericError, size);
	}

	public string NormalizedUrl { get; }
	public string DownloadUrl { get; }
	public DownloadStatus Status { get; }
	public ulong Size { get; }
	public byte[]? Data { get; }

	internal Download(string normalizedUrl, string downloadUrl, DownloadStatus status, ulong size, byte[]? data = null) {
		NormalizedUrl = normalizedUrl;
		DownloadUrl = downloadUrl;
		Status = status;
		Size = size;
		Data = data;
	}

	internal Download WithData(byte[] data) {
		return new Download(NormalizedUrl, DownloadUrl, Status, Size, data);
	}
}
