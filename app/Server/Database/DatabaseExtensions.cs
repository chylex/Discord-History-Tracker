using System.Collections.Generic;
using DHT.Server.Data;

namespace DHT.Server.Database;

public static class DatabaseExtensions {
	public static void AddFrom(this IDatabaseFile target, IDatabaseFile source) {
		target.AddServers(source.GetAllServers());
		target.AddChannels(source.GetAllChannels());
		target.AddUsers(source.GetAllUsers().ToArray());
		target.AddMessages(source.GetMessages().ToArray());

		foreach (var download in source.GetDownloadsWithoutData()) {
			target.AddDownload(download.Status == DownloadStatus.Success ? source.GetDownloadWithData(download) : download);
		}
	}

	internal static void AddServers(this IDatabaseFile target, IEnumerable<Data.Server> servers) {
		foreach (var server in servers) {
			target.AddServer(server);
		}
	}

	internal static void AddChannels(this IDatabaseFile target, IEnumerable<Channel> channels) {
		foreach (var channel in channels) {
			target.AddChannel(channel);
		}
	}
}
