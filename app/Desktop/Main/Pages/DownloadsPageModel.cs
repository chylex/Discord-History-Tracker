using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CommunityToolkit.Mvvm.ComponentModel;
using DHT.Desktop.Common;
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

namespace DHT.Desktop.Main.Pages;

sealed partial class DownloadsPageModel : ObservableObject, IAsyncDisposable {
	private static readonly Log Log = Log.ForType<DownloadsPageModel>();
	
	[ObservableProperty(Setter = Access.Private)]
	private bool isToggleDownloadButtonEnabled = true;
	
	public string ToggleDownloadButtonText => IsDownloading ? "Stop Downloading" : "Start Downloading";
	
	[ObservableProperty(Setter = Access.Private)]
	[NotifyPropertyChangedFor(nameof(IsRetryFailedOnDownloadsButtonEnabled))]
	private bool isRetryingFailedDownloads = false;
	
	[ObservableProperty(Setter = Access.Private)]
	[NotifyPropertyChangedFor(nameof(IsRetryFailedOnDownloadsButtonEnabled))]
	private bool hasFailedDownloads;
	
	public bool IsRetryFailedOnDownloadsButtonEnabled => !IsRetryingFailedDownloads && HasFailedDownloads;
	
	[ObservableProperty(Setter = Access.Private)]
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
		
		OnPropertyChanged(nameof(ToggleDownloadButtonText));
		OnPropertyChanged(nameof(IsDownloading));
	}
	
	private void OnItemFinished(DownloadItem item) {
		RecomputeDownloadStatistics();
	}
	
	public async Task OnClickRetryFailedDownloads() {
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
	
	private const string DeleteOrphanedDownloadsTitle = "Delete Orphaned Downloads";
	
	public async Task OnClickDeleteOrphanedDownloads() {
		await ProgressDialog.Show(window, DeleteOrphanedDownloadsTitle, DeleteOrphanedDownloads);
	}
	
	private async Task DeleteOrphanedDownloads(ProgressDialog dialog, IProgressCallback callback) {
		await callback.UpdateIndeterminate("Searching for orphaned downloads...");
		
		HashSet<string> reachableNormalizedUrls = [];
		HashSet<string> orphanedNormalizedUrls = [];
		
		await foreach (Download download in state.Db.Downloads.FindAllDownloadableUrls()) {
			reachableNormalizedUrls.Add(download.NormalizedUrl);
		}
		
		await foreach (Download download in state.Db.Downloads.Get()) {
			string normalizedUrl = download.NormalizedUrl;
			if (!reachableNormalizedUrls.Contains(normalizedUrl)) {
				orphanedNormalizedUrls.Add(normalizedUrl);
			}
		}
		
		if (orphanedNormalizedUrls.Count == 0) {
			await Dialog.ShowOk(window, DeleteOrphanedDownloadsTitle, "No orphaned downloads found.");
			return;
		}
		
		if (await Dialog.ShowYesNo(window, DeleteOrphanedDownloadsTitle, orphanedNormalizedUrls.Count + " orphaned download(s) will be removed from this database. This action cannot be undone. Proceed?") != DialogResult.YesNo.Yes) {
			return;
		}
		
		await callback.UpdateIndeterminate("Deleting orphaned downloads...");
		await state.Db.Downloads.Remove(orphanedNormalizedUrls);
		RecomputeDownloadStatistics();
		
		if (await Dialog.ShowYesNo(window, DeleteOrphanedDownloadsTitle, "Orphaned downloads deleted. Vacuum database now to reclaim space?") != DialogResult.YesNo.Yes) {
			return;
		}
		
		await callback.UpdateIndeterminate("Vacuuming database...");
		await state.Db.Vacuum();
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
		
		HasFailedDownloads = statusStatistics.FailedCount > 0;
	}
	
	[ObservableObject]
	public sealed partial class StatisticsRow(string state) {
		public string State { get; } = state;
		
		[ObservableProperty]
		private int items;
		
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(SizeText))]
		private ulong? size;
		
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(SizeText))]
		private bool hasFilesWithUnknownSize;
		
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
