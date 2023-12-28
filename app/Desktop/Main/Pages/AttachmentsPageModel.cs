using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using DHT.Desktop.Common;
using DHT.Desktop.Main.Controls;
using DHT.Server;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Utils.Logging;
using DHT.Utils.Models;
using DHT.Utils.Tasks;

namespace DHT.Desktop.Main.Pages;

sealed class AttachmentsPageModel : BaseModel, IDisposable {
	private static readonly Log Log = Log.ForType<AttachmentsPageModel>();

	private static readonly DownloadItemFilter EnqueuedItemFilter = new () {
		IncludeStatuses = new HashSet<DownloadStatus> {
			DownloadStatus.Enqueued,
			DownloadStatus.Downloading
		}
	};

	private bool isToggleDownloadButtonEnabled = true;

	public bool IsToggleDownloadButtonEnabled {
		get => isToggleDownloadButtonEnabled;
		set => Change(ref isToggleDownloadButtonEnabled, value);
	}

	public string ToggleDownloadButtonText => IsDownloading ? "Stop Downloading" : "Start Downloading";

	private bool isRetryingFailedDownloads = false;

	public bool IsRetryingFailedDownloads {
		get => isRetryingFailedDownloads;
		set {
			isRetryingFailedDownloads = value;
			OnPropertyChanged(nameof(IsRetryFailedOnDownloadsButtonEnabled));
		}
	}

	public bool IsRetryFailedOnDownloadsButtonEnabled => !IsRetryingFailedDownloads && HasFailedDownloads;

	public string DownloadMessage { get; set; } = "";
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
	public bool HasFailedDownloads => statisticsFailed.Items > 0;

	private readonly State state;
	private readonly ThrottledTask enqueueDownloadItemsTask;
	private readonly ThrottledTask<DownloadStatusStatistics> downloadStatisticsTask;

	private IDisposable? finishedItemsSubscription;
	private int doneItemsCount;
	private int initialFinishedCount;
	private int? totalItemsToDownloadCount;

	public AttachmentsPageModel() : this(State.Dummy) {}

	public AttachmentsPageModel(State state) {
		this.state = state;

		FilterModel = new AttachmentFilterPanelModel(state);

		enqueueDownloadItemsTask = new ThrottledTask(RecomputeDownloadStatistics, TaskScheduler.FromCurrentSynchronizationContext());
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
		await state.Db.Downloads.EnqueueDownloadItems(CreateAttachmentFilter());
		RecomputeDownloadStatistics();
	}

	private void EnqueueDownloadItemsLater() {
		var filter = CreateAttachmentFilter();
		enqueueDownloadItemsTask.Post(cancellationToken => state.Db.Downloads.EnqueueDownloadItems(filter, cancellationToken));
	}

	private AttachmentFilter CreateAttachmentFilter() {
		var filter = FilterModel.CreateFilter();
		filter.DownloadItemRule = AttachmentFilter.DownloadItemRules.OnlyNotPresent;
		return filter;
	}

	private void RecomputeDownloadStatistics() {
		downloadStatisticsTask.Post(state.Db.Downloads.GetStatistics);
	}

	private void UpdateStatistics(DownloadStatusStatistics statusStatistics) {
		var hadFailedDownloads = HasFailedDownloads;

		statisticsEnqueued.Items = statusStatistics.EnqueuedCount;
		statisticsEnqueued.Size = statusStatistics.EnqueuedSize;

		statisticsDownloaded.Items = statusStatistics.SuccessfulCount;
		statisticsDownloaded.Size = statusStatistics.SuccessfulSize;

		statisticsFailed.Items = statusStatistics.FailedCount;
		statisticsFailed.Size = statusStatistics.FailedSize;

		statisticsSkipped.Items = statusStatistics.SkippedCount;
		statisticsSkipped.Size = statusStatistics.SkippedSize;

		OnPropertyChanged(nameof(StatisticsRows));

		if (hadFailedDownloads != HasFailedDownloads) {
			OnPropertyChanged(nameof(HasFailedDownloads));
			OnPropertyChanged(nameof(IsRetryFailedOnDownloadsButtonEnabled));
		}

		totalItemsToDownloadCount = statisticsEnqueued.Items + statisticsDownloaded.Items + statisticsFailed.Items - initialFinishedCount;
		UpdateDownloadMessage();
	}

	private void UpdateDownloadMessage() {
		DownloadMessage = IsDownloading ? doneItemsCount.Format() + " / " + (totalItemsToDownloadCount?.Format() ?? "?") : "";

		OnPropertyChanged(nameof(DownloadMessage));
		OnPropertyChanged(nameof(DownloadProgress));
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
			initialFinishedCount = 0;
			totalItemsToDownloadCount = null;
			UpdateDownloadMessage();
		}
		else {
			var finishedItems = await state.Downloader.Start();

			initialFinishedCount = statisticsDownloaded.Items + statisticsFailed.Items;
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

	public sealed class StatisticsRow {
		public string State { get; }
		public int Items { get; set; }
		public ulong? Size { get; set; }

		public StatisticsRow(string state) {
			State = state;
		}
	}
}
