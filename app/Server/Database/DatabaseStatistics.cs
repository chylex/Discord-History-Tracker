using DHT.Utils.Models;

namespace DHT.Server.Database;

/// <summary>
/// A live view of database statistics.
/// Some of the totals are computed asynchronously and may not reflect the most recent version of the database, or may not be available at all until computed for the first time.
/// </summary>
public sealed class DatabaseStatistics : BaseModel {
	private long totalServers;
	private long totalChannels;
	private long totalUsers;
	private long? totalMessages;
	private long? totalAttachments;
	private long? totalDownloads;

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

	public long? TotalAttachments {
		get => totalAttachments;
		internal set => Change(ref totalAttachments, value);
	}

	public long? TotalDownloads {
		get => totalDownloads;
		internal set => Change(ref totalDownloads, value);
	}
}
