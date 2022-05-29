using DHT.Server.Data;

namespace DHT.Server.Database {
	public static class DatabaseExtensions {
		public static void AddFrom(this IDatabaseFile target, IDatabaseFile source) {
			foreach (var server in source.GetAllServers()) {
				target.AddServer(server);
			}

			foreach (var channel in source.GetAllChannels()) {
				target.AddChannel(channel);
			}

			target.AddUsers(source.GetAllUsers().ToArray());
			target.AddMessages(source.GetMessages().ToArray());
			
			foreach (var download in source.GetDownloadsWithoutData()) {
				target.AddDownload(download.Status == DownloadStatus.Success ? source.GetDownloadWithData(download) : download);
			}
		}
	}
}
