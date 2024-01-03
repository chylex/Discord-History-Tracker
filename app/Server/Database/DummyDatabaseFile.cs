using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DHT.Server.Database.Repositories;

namespace DHT.Server.Database;

[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
sealed class DummyDatabaseFile : IDatabaseFile {
	public static DummyDatabaseFile Instance { get; } = new ();

	public string Path => "";
	
	public IUserRepository Users { get; } = new IUserRepository.Dummy();
	public IServerRepository Servers { get; } = new IServerRepository.Dummy();
	public IChannelRepository Channels { get; } = new IChannelRepository.Dummy();
	public IMessageRepository Messages { get; } = new IMessageRepository.Dummy();
	public IDownloadRepository Downloads { get; } = new IDownloadRepository.Dummy();
	
	private DummyDatabaseFile() {}

	public Task Vacuum() {
		return Task.CompletedTask;
	}

	public ValueTask DisposeAsync() {
		return ValueTask.CompletedTask;
	}
}
