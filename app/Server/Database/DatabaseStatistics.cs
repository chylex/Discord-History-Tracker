using DHT.Utils.Models;

namespace DHT.Server.Database {
	public sealed class DatabaseStatistics : BaseModel {
		private long totalServers;
		private long totalChannels;
		private long totalUsers;
		private long? totalMessages;

		public long TotalServers {
			get => totalServers;
			internal set => Change(ref totalServers, value);
		}

		public long TotalChannels {
			get => totalChannels;
			internal set => Change(ref totalChannels, value);
		}

		public long TotalUsers {
			get => totalUsers;
			internal set => Change(ref totalUsers, value);
		}

		public long? TotalMessages {
			get => totalMessages;
			internal set => Change(ref totalMessages, value);
		}

		public DatabaseStatistics Clone() {
			return new DatabaseStatistics {
				totalServers = totalServers,
				totalChannels = totalChannels,
				totalUsers = TotalUsers,
				totalMessages = totalMessages
			};
		}
	}
}
