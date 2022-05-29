using System;
using System.Collections.Generic;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Download;

namespace DHT.Server.Database {
	public interface IDatabaseFile : IDisposable {
		string Path { get; }
		DatabaseStatistics Statistics { get; }
		DatabaseStatisticsSnapshot SnapshotStatistics();

		void AddServer(Data.Server server);
		List<Data.Server> GetAllServers();

		void AddChannel(Channel channel);
		List<Channel> GetAllChannels();

		void AddUsers(User[] users);
		List<User> GetAllUsers();

		void AddMessages(Message[] messages);
		int CountMessages(MessageFilter? filter = null);
		List<Message> GetMessages(MessageFilter? filter = null);
		void RemoveMessages(MessageFilter filter, FilterRemovalMode mode);

		int CountAttachments(AttachmentFilter? filter = null);

		void AddDownloads(IEnumerable<Data.Download> downloads);
		void EnqueueDownloadItems(AttachmentFilter? filter = null);
		List<DownloadItem> GetEnqueuedDownloadItems(int count);
		void RemoveDownloadItems(DownloadItemFilter? filter, FilterRemovalMode mode);
		DownloadStatusStatistics GetDownloadStatusStatistics();

		void Vacuum();
	}
}
