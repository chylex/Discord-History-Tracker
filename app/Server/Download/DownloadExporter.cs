using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Utils.Logging;
using DHT.Utils.Tasks;
using Channel = System.Threading.Channels.Channel;

namespace DHT.Server.Download;

public sealed partial class DownloadExporter(IDatabaseFile db, string folderPath) {
	private static readonly Log Log = Log.ForType<DownloadExporter>();
	
	private const int Concurrency = 3;
	
	private static Channel<Data.Download> CreateExportChannel() {
		return Channel.CreateBounded<Data.Download>(new BoundedChannelOptions(Concurrency * 4) {
			SingleWriter = true,
			SingleReader = false,
			AllowSynchronousContinuations = true,
			FullMode = BoundedChannelFullMode.Wait,
		});
	}
	
	public interface IProgressReporter {
		Task ReportProgress(long processedCount, long totalCount);
	}
	
	public readonly record struct Result(long SuccessfulCount, long FailedCount) {
		internal static Result Combine(Result left, Result right) {
			return new Result(left.SuccessfulCount + right.SuccessfulCount, left.FailedCount + right.FailedCount);
		}
	}
	
	public async Task<Result> Export(IProgressReporter reporter) {
		DownloadItemFilter filter = new DownloadItemFilter {
			IncludeStatuses = [DownloadStatus.Success]
		};
		
		long totalCount = await db.Downloads.Count(filter);
		
		Channel<Data.Download> channel = CreateExportChannel();
		ExportRunner exportRunner = new ExportRunner(db, folderPath, channel.Reader, reporter, totalCount);
		
		using CancellableTask progressTask = CancellableTask.Run(exportRunner.RunReportTask);
		
		List<Task<Result>> readerTasks = [];
		for (int reader = 0; reader < Concurrency; reader++) {
			readerTasks.Add(Task.Run(exportRunner.RunExportTask, CancellationToken.None));
		}
		
		await foreach (Data.Download download in db.Downloads.Get(filter).WithCancellation(CancellationToken.None)) {
			await channel.Writer.WriteAsync(download, CancellationToken.None);
		}
		
		channel.Writer.Complete();
		
		Result result = (await Task.WhenAll(readerTasks)).Aggregate(Result.Combine);
		
		progressTask.Cancel();
		await progressTask.Task;
		
		return result;
	}
	
	private sealed partial class ExportRunner(IDatabaseFile db, string folderPath, ChannelReader<Data.Download> reader, IProgressReporter reporter, long totalCount) {
		private long processedCount;
		
		public async Task RunReportTask(CancellationToken cancellationToken) {
			try {
				while (true) {
					await reporter.ReportProgress(processedCount, totalCount);
					await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);
				}
			} catch (OperationCanceledException) {
				await reporter.ReportProgress(processedCount, totalCount);
			}
		}
		
		public async Task<Result> RunExportTask() {
			long successfulCount = 0L;
			long failedCount = 0L;
			
			await foreach (Data.Download download in reader.ReadAllAsync()) {
				bool success;
				try {
					success = await db.Downloads.GetDownloadData(download.NormalizedUrl, stream => CopyToFile(download.NormalizedUrl, stream));
				} catch (FileAlreadyExistsException) {
					success = false;
				} catch (Exception e) {
					Log.Error("Could not export downloaded file: " + download.NormalizedUrl, e);
					success = false;
				}
				
				if (success) {
					++successfulCount;
				}
				else {
					++failedCount;
				}
				
				Interlocked.Increment(ref processedCount);
			}
			
			return new Result(successfulCount, failedCount);
		}
		
		private async Task CopyToFile(string normalizedUrl, Stream blobStream) {
			string fileName = UrlToFileName(normalizedUrl);
			string filePath = Path.Combine(folderPath, fileName);
			
			if (File.Exists(filePath)) {
				Log.Error("Skipping existing file: " + fileName);
				throw FileAlreadyExistsException.Instance;
			}
			
			await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
			await blobStream.CopyToAsync(fileStream);
		}
		
		[GeneratedRegex("[^a-zA-Z0-9_.-]")]
		private static partial Regex DisallowedFileNameCharactersRegex();
		
		private static string UrlToFileName(string url) {
			static string UriToFileName(Uri uri) {
				string fileName = uri.AbsolutePath.TrimStart('/');
				
				if (uri.Query.Length > 0) {
					int periodIndex = fileName.LastIndexOf('.');
					return fileName.Insert(periodIndex == -1 ? fileName.Length : periodIndex, uri.Query.TrimEnd('&'));
				}
				else {
					return fileName;
				}
			}
			
			string fileName = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? UriToFileName(uri) : url;
			return DisallowedFileNameCharactersRegex().Replace(fileName, "_");
		}
	}
	
	private sealed class FileAlreadyExistsException : Exception {
		public static FileAlreadyExistsException Instance { get; } = new ();
	}
}
