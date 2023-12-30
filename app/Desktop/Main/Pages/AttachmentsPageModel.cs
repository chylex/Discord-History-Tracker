using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using CommunityToolkit.Mvvm.ComponentModel;
using DHT.Desktop.Common;
using DHT.Desktop.Main.Controls;
using DHT.Server;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Utils.Logging;
using DHT.Utils.Tasks;

namespace DHT.Desktop.Main.Pages;

sealed partial class AttachmentsPageModel : ObservableObject, IDisposable {
	private static readonly Log Log = Log.ForType<AttachmentsPageModel>();

	private static readonly DownloadItemFilter EnqueuedItemFilter = new () {
		IncludeStatuses = new HashSet<DownloadStatus> {
			DownloadStatus.Enqueued,
			DownloadStatus.Downloading
		}
	};

	[ObservableProperty(Setter = Access.Private)]
	private bool isToggleDownloadButtonEnabled = true;

	public string ToggleDownloadButtonText => IsDownloading ? "Stop Downloading" : "Start Downloading";

	[ObservableProperty(Setter = Access.Private)]
	[NotifyPropertyChangedFor(nameof(IsRetryFailedOnDownloadsButtonEnabled))]
	private bool isRetryingFailedDownloads = false;

	[ObservableProperty(Setter = Access.Private)]
	[NotifyPropertyChangedFor(nameof(IsRetryFailedOnDownloadsButtonEnabled))]
	private bool hasFailedDownloads;
	
	public bool IsRetryFailedOnDownloadsButtonEnabled => !IsRetryingFailedDownloads && hasFailedDownloads;

	[ObservableProperty(Setter = Access.Private)]
	private string downloadMessage = "";
	
	public double DownloadProgress => totalItemsToDownloadCount is null or 0 ? 0.0 : 100.0 * doneItemsCount / totalItemsToDownloadCount.Value;

	public AttachmentFilterPanelModel FilterModel { get; }

	private readonly StatisticsRow statisticsEnqueued = new ("Enqueued");
	private readonly StatisticsRow statisticsDownloaded = new ("Downloaded");
	private readonly StatisticsRow statisticsFailed = new ("Failed");
	private readonly StatisticsRow statisticsSkipped = new ("Skipped");

	public List<StatisticsRow> StatisticsRows => [
		statisticsEnqueued,
		statisticsDownloaded,
		statisticsFailed,
		statisticsSkipped
	];

	public bool IsDownloading => state.Downloader.IsDownloading;

	private readonly State state;
	private readonly ThrottledTask<int> enqueueDownloadItemsTask;
	private readonly ThrottledTask<DownloadStatusStatistics> downloadStatisticsTask;

	private IDisposable? finishedItemsSubscription;
	private int doneItemsCount;
	private int totalEnqueuedItemCount;
	private int? totalItemsToDownloadCount;

	public AttachmentsPageModel() : this(State.Dummy) {}

	public AttachmentsPageModel(State state) {
		this.state = state;

		FilterModel = new AttachmentFilterPanelModel(state);

		enqueueDownloadItemsTask = new ThrottledTask<int>(OnItemsEnqueued, TaskScheduler.FromCurrentSynchronizationContext());
		downloadStatisticsTask = new ThrottledTask<DownloadStatusStatistics>(UpdateStatistics, TaskScheduler.FromCurrentSynchronizationContext());
		RecomputeDownloadStatistics();

		state.Db.Statistics.PropertyChanged += OnDbStatisticsChanged;
	}

	public void Dispose() {
		state.Db.Statistics.PropertyChanged -= OnDbStatisticsChanged;
		enqueueDownloadItemsTask.Dispose();
		downloadStatisticsTask.Dispose();
		finishedItemsSubscription?.Dispose();
		FilterModel.Dispose();
	}

	private void OnDbStatisticsChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName == nameof(DatabaseStatistics.TotalAttachments)) {
			if (IsDownloading) {
				EnqueueDownloadItemsLater();
			}
			else {
				RecomputeDownloadStatistics();
			}
		}
		else if (e.PropertyName == nameof(DatabaseStatistics.TotalDownloads)) {
			RecomputeDownloadStatistics();
		}
	}

	private async Task EnqueueDownloadItems() {
		OnItemsEnqueued(await state.Db.Downloads.EnqueueDownloadItems(CreateAttachmentFilter()));
	}

	private void EnqueueDownloadItemsLater() {
		var filter = CreateAttachmentFilter();
		enqueueDownloadItemsTask.Post(cancellationToken => state.Db.Downloads.EnqueueDownloadItems(filter, cancellationToken));
	}

	private void OnItemsEnqueued(int itemCount) {
		totalEnqueuedItemCount += itemCount;
		totalItemsToDownloadCount = totalEnqueuedItemCount;
		UpdateDownloadMessage();
		RecomputeDownloadStatistics();
	}

	private AttachmentFilter CreateAttachmentFilter() {
		var filter = FilterModel.CreateFilter();
		filter.DownloadItemRule = AttachmentFilter.DownloadItemRules.OnlyNotPresent;
		return filter;
	}

	public async Task OnClickToggleDownload() {
		IsToggleDownloadButtonEnabled = false;

		if (IsDownloading) {
			await state.Downloader.Stop();

			finishedItemsSubscription?.Dispose();
			finishedItemsSubscription = null;

			RecomputeDownloadStatistics();

			await state.Db.Downloads.RemoveDownloadItems(EnqueuedItemFilter, FilterRemovalMode.RemoveMatching);

			doneItemsCount = 0;
			totalEnqueuedItemCount = 0;
			totalItemsToDownloadCount = null;
			UpdateDownloadMessage();
		}
		else {
			var finishedItems = await state.Downloader.Start();

			finishedItemsSubscription = finishedItems.Select(static _ => true)
			                                         .Buffer(TimeSpan.FromMilliseconds(100))
			                                         .Select(static items => items.Count)
			                                         .Where(static items => items > 0)
			                                         .ObserveOn(AvaloniaScheduler.Instance)
			                                         .Subscribe(OnItemsFinished);

			await EnqueueDownloadItems();
		}

		OnPropertyChanged(nameof(ToggleDownloadButtonText));
		OnPropertyChanged(nameof(IsDownloading));
		IsToggleDownloadButtonEnabled = true;
	}

	private void OnItemsFinished(int finishedItemCount) {
		doneItemsCount += finishedItemCount;
		UpdateDownloadMessage();
		RecomputeDownloadStatistics();
	}

	public async Task OnClickRetryFailedDownloads() {
		IsRetryingFailedDownloads = true;

		try {
			var allExceptFailedFilter = new DownloadItemFilter {
				IncludeStatuses = new HashSet<DownloadStatus> {
					DownloadStatus.Enqueued,
					DownloadStatus.Downloading,
					DownloadStatus.Success
				}
			};

			await state.Db.Downloads.RemoveDownloadItems(allExceptFailedFilter, FilterRemovalMode.KeepMatching);

			if (IsDownloading) {
				await EnqueueDownloadItems();
			}
		} catch (Exception e) {
			Log.Error(e);
		} finally {
			IsRetryingFailedDownloads = false;
		}
	}

	private void RecomputeDownloadStatistics() {
		downloadStatisticsTask.Post(state.Db.Downloads.GetStatistics);
	}

	private void UpdateStatistics(DownloadStatusStatistics statusStatistics) {
		statisticsEnqueued.Items = statusStatistics.EnqueuedCount;
		statisticsEnqueued.Size = statusStatistics.EnqueuedSize;

		statisticsDownloaded.Items = statusStatistics.SuccessfulCount;
		statisticsDownloaded.Size = statusStatistics.SuccessfulSize;

		statisticsFailed.Items = statusStatistics.FailedCount;
		statisticsFailed.Size = statusStatistics.FailedSize;

		statisticsSkipped.Items = statusStatistics.SkippedCount;
		statisticsSkipped.Size = statusStatistics.SkippedSize;

		OnPropertyChanged(nameof(StatisticsRows));

		hasFailedDownloads = statusStatistics.FailedCount > 0;

		UpdateDownloadMessage();
	}

	private void UpdateDownloadMessage() {
		DownloadMessage = IsDownloading ? doneItemsCount.Format() + " / " + (totalItemsToDownloadCount?.Format() ?? "?") : "";

		OnPropertyChanged(nameof(DownloadProgress));
	}

	public sealed class StatisticsRow {
		public string State { get; }
		public int Items { get; set; }
		public ulong? Size { get; set; }

		public StatisticsRow(string state) {
			State = state;
		}
	}
}
