using System.Net;

namespace DHT.Server.Data;

/// <summary>
/// Extends <see cref="HttpStatusCode"/> with custom status codes in the range 0-99.
/// </summary>
public enum DownloadStatus {
	Enqueued = 0,
	GenericError = 1,
	Success = HttpStatusCode.OK
}
