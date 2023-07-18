using System.Collections.Generic;

namespace DHT.Server.Data.Filters;

public sealed class DownloadItemFilter {
	public HashSet<DownloadStatus>? IncludeStatuses { get; init; } = null;
	public HashSet<DownloadStatus>? ExcludeStatuses { get; init; } = null;

	public bool IsEmpty => IncludeStatuses == null && ExcludeStatuses == null;
}
