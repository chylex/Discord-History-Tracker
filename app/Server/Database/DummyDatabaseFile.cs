using System.Collections.Generic;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Download;

namespace DHT.Server.Database {
	public sealed class DummyDatabaseFile : IDatabaseFile {
		public static DummyDatabaseFile Instance { get; } = new();

		public string Path => "";
		public DatabaseStatistics Statistics { get; } = new();

		private DummyDatabaseFile() {}

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

		public void RemoveMessages(MessageFilter filter, FilterRemovalMode mode) {}

		public int CountAttachments(AttachmentFilter? filter = null) {
			return new();
		}

		public void AddDownloads(IEnumerable<Data.Download> downloads) {}

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
}
