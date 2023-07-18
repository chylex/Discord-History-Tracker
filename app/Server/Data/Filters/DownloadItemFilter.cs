using System.Collections.Generic;

namespace DHT.Server.Data.Filters;

public sealed class DownloadItemFilter {
	public HashSet<DownloadStatus>? IncludeStatuses { get; set; } = null;
	public HashSet<DownloadStatus>? ExcludeStatuses { get; set; } = null;

	public bool IsEmpty => IncludeStatuses == null && ExcludeStatuses == null;
}
