using System;
using System.Collections.Generic;

namespace DHT.Server.Data.Filters {
	public class MessageFilter {
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }

		public HashSet<ulong>? ChannelIds { get; set; } = null;
		public HashSet<ulong>? UserIds { get; set; } = null;
		public HashSet<ulong>? MessageIds { get; set; } = null;
	}
}
