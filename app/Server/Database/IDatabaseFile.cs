using System;
using System.Threading.Tasks;
using DHT.Server.Database.Repositories;

namespace DHT.Server.Database;

public interface IDatabaseFile : IDisposable {
	string Path { get; }
	DatabaseStatistics Statistics { get; }
	Task<DatabaseStatisticsSnapshot> SnapshotStatistics();

	IUserRepository Users { get; }
	IServerRepository Servers { get; }
	IChannelRepository Channels { get; }
	IMessageRepository Messages { get; }
	IDownloadRepository Downloads { get; }

	Task Vacuum();
}
