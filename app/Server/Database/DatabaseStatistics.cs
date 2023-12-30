using CommunityToolkit.Mvvm.ComponentModel;

namespace DHT.Server.Database;

/// <summary>
/// A live view of database statistics.
/// Some of the totals are computed asynchronously and may not reflect the most recent version of the database, or may not be available at all until computed for the first time.
/// </summary>
public sealed partial class DatabaseStatistics : ObservableObject {
	[ObservableProperty(Setter = Access.Internal)]
	private long totalServers;
	
	[ObservableProperty(Setter = Access.Internal)]
	private long totalChannels;
	
	[ObservableProperty(Setter = Access.Internal)]
	private long totalUsers;
	
	[ObservableProperty(Setter = Access.Internal)]
	private long? totalMessages;
	
	[ObservableProperty(Setter = Access.Internal)]
	private long? totalAttachments;
	
	[ObservableProperty(Setter = Access.Internal)]
	private long? totalDownloads;
}
