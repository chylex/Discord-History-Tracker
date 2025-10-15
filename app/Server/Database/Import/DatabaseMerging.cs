using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DHT.Server.Data;

namespace DHT.Server.Database.Import;

public static class DatabaseMerging {
	public static async Task Merge(this IDatabaseFile target, IDatabaseFile source, IProgressCallback callback) {
		callback.OnImportingMetadata();
		
		await target.Users.Add(await source.Users.Get().ToListAsync());
		await target.Servers.Add(await source.Servers.Get().ToListAsync());
		await target.Channels.Add(await source.Channels.Get().ToListAsync());
		
		await MergeMessages(target, source, callback);
		await MergeDownloads(target, source, callback);
	}
	
	private static async Task MergeMessages(IDatabaseFile target, IDatabaseFile source, IProgressCallback callback) {
		const int MessageBatchSize = 100;
		const int ReportEveryBatches = 10;
		List<Message> batchedMessages = new (MessageBatchSize);
		
		long totalMessages = await source.Messages.Count();
		long importedMessages = 0;
		
		callback.OnMessagesImported(importedMessages, totalMessages);
		
		await foreach (Message message in source.Messages.Get()) {
			batchedMessages.Add(message);
			
			if (batchedMessages.Count >= MessageBatchSize) {
				await target.Messages.Add(batchedMessages);
				
				importedMessages += batchedMessages.Count;
				
				if (importedMessages % (MessageBatchSize * ReportEveryBatches) == 0) {
					callback.OnMessagesImported(importedMessages, totalMessages);
				}
				
				batchedMessages.Clear();
			}
		}
		
		await target.Messages.Add(batchedMessages);
		callback.OnMessagesImported(totalMessages, totalMessages);
	}
	
	private static async Task MergeDownloads(IDatabaseFile target, IDatabaseFile source, IProgressCallback callback) {
		const int ReportBatchSize = 100;
		
		long totalDownloads = await source.Downloads.Count();
		long importedDownloads = 0;
		
		callback.OnDownloadsImported(importedDownloads, totalDownloads);
		
		await foreach (Data.Download download in source.Downloads.Get()) {
			if (download.Status != DownloadStatus.Success || !await source.Downloads.GetDownloadData(download.NormalizedUrl, stream => target.Downloads.AddDownload(download, stream))) {
				await target.Downloads.AddDownload(download, stream: null);
			}
			
			if (++importedDownloads % ReportBatchSize == 0) {
				callback.OnDownloadsImported(importedDownloads, totalDownloads);
			}
		}
		
		callback.OnDownloadsImported(totalDownloads, totalDownloads);
	}
	
	public interface IProgressCallback {
		void OnImportingMetadata();
		void OnMessagesImported(long finished, long total);
		void OnDownloadsImported(long finished, long total);
	}
}
