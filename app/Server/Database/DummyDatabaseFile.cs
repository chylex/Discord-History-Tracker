using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Download;

namespace DHT.Server.Database;

[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
public sealed class DummyDatabaseFile : IDatabaseFile {
	public static DummyDatabaseFile Instance { get; } = new();

	public string Path => "";
	public DatabaseStatistics Statistics { get; } = new();

	private DummyDatabaseFile() {}

	public DatabaseStatisticsSnapshot SnapshotStatistics() {
		return new();
	}

	public void AddServer(Data.Server server) {}

	public List<Data.Server> GetAllServers() {
		return new();
	}

	public void AddChannel(Channel channel) {}

	public List<Channel> GetAllChannels() {
		return new();
	}

	public void AddUsers(User[] users) {}

	public List<User> GetAllUsers() {
		return new();
	}

	public void AddMessages(Message[] messages) {}

	public int CountMessages(MessageFilter? filter = null) {
		return 0;
	}

	public List<Message> GetMessages(MessageFilter? filter = null) {
		return new();
	}

	public HashSet<ulong> GetMessageIds(MessageFilter? filter = null) {
		return new();
	}

	public void RemoveMessages(MessageFilter filter, FilterRemovalMode mode) {}

	public int CountAttachments(AttachmentFilter? filter = null) {
		return new();
	}

	public List<Data.Download> GetDownloadsWithoutData() {
		return new();
	}

	public Data.Download GetDownloadWithData(Data.Download download) {
		return download;
	}

	public DownloadedAttachment? GetDownloadedAttachment(string url) {
		return null;
	}

	public void AddDownload(Data.Download download) {}

	public void EnqueueDownloadItems(AttachmentFilter? filter = null) {}

	public List<DownloadItem> GetEnqueuedDownloadItems(int count) {
		return new();
	}

	public void RemoveDownloadItems(DownloadItemFilter? filter, FilterRemovalMode mode) {}

	public DownloadStatusStatistics GetDownloadStatusStatistics() {
		return new();
	}

	public void Vacuum() {}

	public void Dispose() {}
}
