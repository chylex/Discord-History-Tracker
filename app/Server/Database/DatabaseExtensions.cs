using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DHT.Server.Data;

namespace DHT.Server.Database;

public static class DatabaseExtensions {
	public static async Task AddFrom(this IDatabaseFile target, IDatabaseFile source) {
		await target.Users.Add(await source.Users.Get().ToListAsync());
		await target.Servers.Add(await source.Servers.Get().ToListAsync());
		await target.Channels.Add(await source.Channels.Get().ToListAsync());

		const int MessageBatchSize = 100;
		List<Message> batchedMessages = new (MessageBatchSize);
		
		await foreach (var message in source.Messages.Get()) {
			batchedMessages.Add(message);

			if (batchedMessages.Count >= MessageBatchSize) {
				await target.Messages.Add(batchedMessages);
				batchedMessages.Clear();
			}
		}
		
		await target.Messages.Add(batchedMessages);

		await foreach (var download in source.Downloads.Get()) {
			await target.Downloads.AddDownload(await source.Downloads.HydrateWithData(download));
		}
	}
}
