using System;
using System.Threading.Tasks;
using DHT.Server.Database.Repositories;

namespace DHT.Server.Database;

public interface IDatabaseFile : IAsyncDisposable {
	string Path { get; }
	
	ISettingsRepository Settings { get; }
	IUserRepository Users { get; }
	IServerRepository Servers { get; }
	IChannelRepository Channels { get; }
	IMessageRepository Messages { get; }
	IDownloadRepository Downloads { get; }
	
	Task Vacuum();
}
