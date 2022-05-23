using System;
using System.Net;

namespace DHT.Server.Data {
	public readonly struct Download {
		internal static Download NewEnqueued(string url, ulong size) {
			return new Download(url, DownloadStatus.Enqueued, size);
		}

		internal static Download NewSuccess(string url, byte[] data) {
			return new Download(url, DownloadStatus.Success, (ulong) Math.Max(data.LongLength, 0), data);
		}

		internal static Download NewFailure(string url, HttpStatusCode? statusCode, ulong size) {
			return new Download(url, statusCode.HasValue ? (DownloadStatus) (int) statusCode : DownloadStatus.GenericError, size);
		}

		public string Url { get; }
		public DownloadStatus Status { get; }
		public ulong Size { get; }
		public byte[]? Data { get; }

		private Download(string url, DownloadStatus status, ulong size, byte[]? data = null) {
			Url = url;
			Status = status;
			Size = size;
			Data = data;
		}
	}
}
