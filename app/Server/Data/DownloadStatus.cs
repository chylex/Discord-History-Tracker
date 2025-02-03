using System.Net;

namespace DHT.Server.Data;

/// <summary>
/// Extends <see cref="HttpStatusCode"/> with custom status codes in the range 0-99.
/// </summary>
public enum DownloadStatus {
	Pending = 0,
	GenericError = 1,
	Downloading = 2,
	LastCustomCode = 99,
	Success = HttpStatusCode.OK,
}
