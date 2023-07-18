using System;
using System.Collections.Generic;

namespace DHT.Server.Data.Filters;

public sealed class MessageFilter {
	public DateTime? StartDate { get; set; } = null;
	public DateTime? EndDate { get; set; } = null;

	public HashSet<ulong>? ChannelIds { get; set; } = null;
	public HashSet<ulong>? UserIds { get; set; } = null;
	public HashSet<ulong>? MessageIds { get; set; } = null;

	public bool IsEmpty => StartDate == null &&
	                       EndDate == null &&
	                       ChannelIds == null &&
	                       UserIds == null &&
	                       MessageIds == null;
}
