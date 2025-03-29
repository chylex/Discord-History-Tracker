using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.File;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Desktop.Main.Controls;
using DHT.Server;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Data.Settings;
using DHT.Server.Download;
using DHT.Utils.Logging;
using DHT.Utils.Tasks;
using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Main.Pages;

sealed partial class DownloadsPageModel : IAsyncDisposable {
	private static readonly Log Log = Log.ForType<DownloadsPageModel>();
	
	[Notify(Setter.Private)]
	private bool isToggleDownloadButtonEnabled = true;
	
	[DependsOn(nameof(IsDownloading))]
	public string ToggleDownloadButtonText => IsDownloading ? "Stop Downloading" : "Start Downloading";
	
	[Notify(Setter.Private)]
	private bool isRetryingFailedDownloads = false;
	
	[Notify(Setter.Private)]
	private bool hasSuccessfulDownloads;
	
	[Notify(Setter.Private)]
	private bool hasFailedDownloads;
	
	[DependsOn(nameof(IsRetryingFailedDownloads), nameof(HasFailedDownloads))]
	public bool IsRetryFailedOnDownloadsButtonEnabled => !IsRetryingFailedDownloads && HasFailedDownloads;
	
	[Notify(Setter.Private)]
	private string downloadMessage = "";
	
	public DownloadItemFilterPanelModel FilterModel { get; }
	
	private readonly StatisticsRow statisticsPending = new ("Pending");
	private readonly StatisticsRow statisticsDownloaded = new ("Downloaded");
	private readonly StatisticsRow statisticsFailed = new ("Failed");
	private readonly StatisticsRow statisticsSkipped = new ("Skipped");
	
	public ObservableCollection<StatisticsRow> StatisticsRows { get; }
	
	public bool IsDownloading => state.Downloader.IsDownloading;
	
	private readonly Window window;
	private readonly State state;
	private readonly ThrottledTask<DownloadStatusStatistics> downloadStatisticsTask;
	private readonly IDisposable downloadItemCountSubscription;
	
	private IDisposable? finishedItemsSubscription;
	private DownloadItemFilter? currentDownloadFilter;
	
	public DownloadsPageModel() : this(null!, State.Dummy) {}
	
	public DownloadsPageModel(Window window, State state) {
		this.window = window;
		this.state = state;
		
		FilterModel = new DownloadItemFilterPanelModel(state);
		
		StatisticsRows = [
			statisticsPending,
			statisticsDownloaded,
			statisticsFailed,
			statisticsSkipped,
		];
		
		downloadStatisticsTask = new ThrottledTask<DownloadStatusStatistics>(Log, UpdateStatistics, TaskScheduler.FromCurrentSynchronizationContext());
		downloadItemCountSubscription = state.Db.Downloads.TotalCount.ObserveOn(AvaloniaScheduler.Instance).Subscribe(OnDownloadCountChanged);
		
		RecomputeDownloadStatistics();
	}
	
	public async Task Initialize() {
		await FilterModel.Initialize();
		
		if (await state.Db.Settings.Get(SettingsKey.DownloadsAutoStart, defaultValue: false)) {
			await StartDownload();
		}
	}
	
	public async ValueTask DisposeAsync() {
		finishedItemsSubscription?.Dispose();
		
		downloadItemCountSubscription.Dispose();
		downloadStatisticsTask.Dispose();
		
		await FilterModel.DisposeAsync();
	}
	
	private void OnDownloadCountChanged(long newDownloadCount) {
		RecomputeDownloadStatistics();
	}
	
	public async Task OnClickToggleDownload() {
		IsToggleDownloadButtonEnabled = false;
		
		if (IsDownloading) {
			await StopDownload();
		}
		else {
			await StartDownload();
		}
		
		await state.Db.Settings.Set(SettingsKey.DownloadsAutoStart, IsDownloading);
		IsToggleDownloadButtonEnabled = true;
	}
	
	private async Task StartDownload() {
		await state.Db.Downloads.MoveDownloadingItemsBackToQueue();
		
		IObservable<DownloadItem> finishedItems = await state.Downloader.Start(currentDownloadFilter = FilterModel.CreateFilter());
		finishedItemsSubscription = finishedItems.ObserveOn(AvaloniaScheduler.Instance).Subscribe(OnItemFinished);
		
		OnDownloadStateChanged();
	}
	
	private async Task StopDownload() {
		await state.Downloader.Stop();
		await state.Db.Downloads.MoveDownloadingItemsBackToQueue();
		
		finishedItemsSubscription?.Dispose();
		finishedItemsSubscription = null;
		
		currentDownloadFilter = null;
		OnDownloadStateChanged();
	}
	
	private void OnDownloadStateChanged() {
		RecomputeDownloadStatistics();
		
		OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsDownloading)));
	}
	
	private void OnItemFinished(DownloadItem item) {
		RecomputeDownloadStatistics();
	}
	
	public async Task OnClickRetryFailed() {
		IsRetryingFailedDownloads = true;
		
		try {
			await state.Db.Downloads.RetryFailed();
			RecomputeDownloadStatistics();
		} catch (Exception e) {
			Log.Error(e);
		} finally {
			IsRetryingFailedDownloads = false;
		}
	}
	
	private void RecomputeDownloadStatistics() {
		downloadStatisticsTask.Post(cancellationToken => state.Db.Downloads.GetStatistics(currentDownloadFilter ?? new DownloadItemFilter(), cancellationToken));
	}
	
	public async Task OnClickDeleteOrphaned() {
		const string Title = "Delete Orphaned Downloads";
		
		try {
			await ProgressDialog.Show(window, Title, async (_, callback) => {
				await callback.UpdateIndeterminate("Searching for orphaned downloads...");
				
				HashSet<string> reachableNormalizedUrls = [];
				HashSet<string> orphanedNormalizedUrls = [];
				
				await foreach (FileUrl fileUrl in state.Db.Downloads.FindReachableFiles()) {
					reachableNormalizedUrls.Add(fileUrl.NormalizedUrl);
				}
				
				await foreach (Download download in state.Db.Downloads.Get()) {
					string normalizedUrl = download.NormalizedUrl;
					if (!reachableNormalizedUrls.Contains(normalizedUrl)) {
						orphanedNormalizedUrls.Add(normalizedUrl);
					}
				}
				
				if (orphanedNormalizedUrls.Count == 0) {
					await Dialog.ShowOk(window, Title, "No orphaned downloads found.");
					return;
				}
				
				if (await Dialog.ShowYesNo(window, Title, orphanedNormalizedUrls.Count + " orphaned download(s) will be removed from this database. This action cannot be undone. Proceed?") != DialogResult.YesNo.Yes) {
					return;
				}
				
				await callback.UpdateIndeterminate("Deleting orphaned downloads...");
				await state.Db.Downloads.Remove(orphanedNormalizedUrls);
				RecomputeDownloadStatistics();
				
				if (await Dialog.ShowYesNo(window, Title, "Orphaned downloads deleted. Vacuum database now to reclaim space?") != DialogResult.YesNo.Yes) {
					return;
				}
				
				await callback.UpdateIndeterminate("Vacuuming database...");
				await state.Db.Vacuum();
			});
		} catch (Exception e) {
			Log.Error(e);
			await Dialog.ShowOk(window, Title, "Could not delete orphaned downloads: " + e.Message);
		}
	}
	
	public async Task OnClickExportAll() {
		const string Title = "Export Downloaded Files";
		
		string[] folders = await window.StorageProvider.OpenFolders(new FolderPickerOpenOptions {
			Title = Title,
			AllowMultiple = false,
		});
		
		if (folders.Length != 1) {
			return;
		}
		
		string folderPath = folders[0];
		
		DownloadExporter exporter = new DownloadExporter(state.Db, folderPath);
		DownloadExporter.Result result;
		try {
			result = await ProgressDialog.Show(window, Title, async (_, callback) => {
				await callback.UpdateIndeterminate("Exporting downloaded files...");
				return await exporter.Export(new ExportProgressReporter(callback));
			});
		} catch (Exception e) {
			Log.Error(e);
			await Dialog.ShowOk(window, Title, "Could not export downloaded files: " + e.Message);
			return;
		}
		
		string messageStart = "Exported " + result.SuccessfulCount.Pluralize("file");
		
		if (result.FailedCount > 0L) {
			await Dialog.ShowOk(window, Title, messageStart + " (" + result.FailedCount.Format() + " failed).");
		}
		else {
			await Dialog.ShowOk(window, Title, messageStart + ".");
		}
	}
	
	private sealed class ExportProgressReporter(IProgressCallback callback) : DownloadExporter.IProgressReporter {
		public Task ReportProgress(long processedCount, long totalCount) {
			return callback.Update("Exporting downloaded files...", processedCount, totalCount);
		}
	}
	
	private void UpdateStatistics(DownloadStatusStatistics statusStatistics) {
		statisticsPending.Items = statusStatistics.PendingCount;
		statisticsPending.Size = statusStatistics.PendingTotalSize;
		statisticsPending.HasFilesWithUnknownSize = statusStatistics.PendingWithUnknownSizeCount > 0;
		
		statisticsDownloaded.Items = statusStatistics.SuccessfulCount;
		statisticsDownloaded.Size = statusStatistics.SuccessfulTotalSize;
		statisticsDownloaded.HasFilesWithUnknownSize = statusStatistics.SuccessfulWithUnknownSizeCount > 0;
		
		statisticsFailed.Items = statusStatistics.FailedCount;
		statisticsFailed.Size = statusStatistics.FailedTotalSize;
		statisticsFailed.HasFilesWithUnknownSize = statusStatistics.FailedWithUnknownSizeCount > 0;
		
		statisticsSkipped.Items = statusStatistics.SkippedCount;
		statisticsSkipped.Size = statusStatistics.SkippedTotalSize;
		statisticsSkipped.HasFilesWithUnknownSize = statusStatistics.SkippedWithUnknownSizeCount > 0;
		
		HasSuccessfulDownloads = statusStatistics.SuccessfulCount > 0;
		HasFailedDownloads = statusStatistics.FailedCount > 0;
	}
	
	public sealed partial class StatisticsRow(string state) {
		public string State { get; } = state;
		
		[Notify]
		private int items;
		
		[Notify]
		private ulong? size;
		
		[Notify]
		private bool hasFilesWithUnknownSize;
		
		[DependsOn(nameof(Size), nameof(HasFilesWithUnknownSize))]
		public string SizeText {
			get {
				if (size == null) {
					return "-";
				}
				else if (hasFilesWithUnknownSize) {
					return "\u2265 " + BytesValueConverter.Convert(size.Value);
				}
				else {
					return BytesValueConverter.Convert(size.Value);
				}
			}
		}
	}
}
