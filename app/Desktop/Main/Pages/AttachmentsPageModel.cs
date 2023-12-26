using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DHT.Desktop.Common;
using DHT.Desktop.Main.Controls;
using DHT.Server;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Server.Download;
using DHT.Utils.Models;
using DHT.Utils.Tasks;

namespace DHT.Desktop.Main.Pages;

sealed class AttachmentsPageModel : BaseModel, IDisposable {
	private static readonly DownloadItemFilter EnqueuedItemFilter = new () {
		IncludeStatuses = new HashSet<DownloadStatus> {
			DownloadStatus.Enqueued,
			DownloadStatus.Downloading
		}
	};

	private bool isThreadDownloadButtonEnabled = true;

	public string ToggleDownloadButtonText => IsDownloading ? "Stop Downloading" : "Start Downloading";

	public bool IsToggleDownloadButtonEnabled {
		get => isThreadDownloadButtonEnabled;
		set => Change(ref isThreadDownloadButtonEnabled, value);
	}

	public string DownloadMessage { get; set; } = "";
	public double DownloadProgress => totalItemsToDownloadCount is null or 0 ? 0.0 : 100.0 * doneItemsCount / totalItemsToDownloadCount.Value;

	public AttachmentFilterPanelModel FilterModel { get; }

	private readonly StatisticsRow statisticsEnqueued = new ("Enqueued");
	private readonly StatisticsRow statisticsDownloaded = new ("Downloaded");
	private readonly StatisticsRow statisticsFailed = new ("Failed");
	private readonly StatisticsRow statisticsSkipped = new ("Skipped");

	public List<StatisticsRow> StatisticsRows {
		get {
			return new List<StatisticsRow> {
				statisticsEnqueued,
				statisticsDownloaded,
				statisticsFailed,
				statisticsSkipped
			};
		}
	}

	public bool IsDownloading => state.Downloader.IsDownloading;
	public bool HasFailedDownloads => statisticsFailed.Items > 0;

	private readonly State state;
	private readonly AsyncValueComputer<DownloadStatusStatistics>.Single downloadStatisticsComputer;

	private int doneItemsCount;
	private int initialFinishedCount;
	private int? totalItemsToDownloadCount;

	public AttachmentsPageModel() : this(State.Dummy) {}

	public AttachmentsPageModel(State state) {
		this.state = state;
		
		FilterModel = new AttachmentFilterPanelModel(state);

		downloadStatisticsComputer = AsyncValueComputer<DownloadStatusStatistics>.WithResultProcessor(UpdateStatistics).WithOutdatedResults().BuildWithComputer(state.Db.GetDownloadStatusStatistics);
		downloadStatisticsComputer.Recompute();

		state.Db.Statistics.PropertyChanged += OnDbStatisticsChanged;
		state.Downloader.OnItemFinished += DownloaderOnOnItemFinished;
	}

	public void Dispose() {
		state.Db.Statistics.PropertyChanged -= OnDbStatisticsChanged;
		state.Downloader.OnItemFinished -= DownloaderOnOnItemFinished;
		FilterModel.Dispose();
	}

	private void OnDbStatisticsChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName == nameof(DatabaseStatistics.TotalAttachments)) {
			if (IsDownloading) {
				EnqueueDownloadItems();
			}
			else {
				downloadStatisticsComputer.Recompute();
			}
		}
		else if (e.PropertyName == nameof(DatabaseStatistics.TotalDownloads)) {
			downloadStatisticsComputer.Recompute();
		}
	}

	private void EnqueueDownloadItems() {
		var filter = FilterModel.CreateFilter();
		filter.DownloadItemRule = AttachmentFilter.DownloadItemRules.OnlyNotPresent;
		state.Db.EnqueueDownloadItems(filter);

		downloadStatisticsComputer.Recompute();
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
		}

		totalItemsToDownloadCount = statisticsEnqueued.Items + statisticsDownloaded.Items + statisticsFailed.Items - initialFinishedCount;
		UpdateDownloadMessage();
	}

	private void UpdateDownloadMessage() {
		DownloadMessage = IsDownloading ? doneItemsCount.Format() + " / " + (totalItemsToDownloadCount?.Format() ?? "?") : "";

		OnPropertyChanged(nameof(DownloadMessage));
		OnPropertyChanged(nameof(DownloadProgress));
	}

	private void DownloaderOnOnItemFinished(object? sender, DownloadItem e) {
		Interlocked.Increment(ref doneItemsCount);
		
		Dispatcher.UIThread.Invoke(UpdateDownloadMessage);
		downloadStatisticsComputer.Recompute();
	}

	public async Task OnClickToggleDownload() {
		if (IsDownloading) {
			IsToggleDownloadButtonEnabled = false;
			await state.Downloader.Stop();
			downloadStatisticsComputer.Recompute();
			IsToggleDownloadButtonEnabled = true;

			state.Db.RemoveDownloadItems(EnqueuedItemFilter, FilterRemovalMode.RemoveMatching);

			doneItemsCount = 0;
			initialFinishedCount = 0;
			totalItemsToDownloadCount = null;
			UpdateDownloadMessage();
		}
		else {
			initialFinishedCount = statisticsDownloaded.Items + statisticsFailed.Items;
			await state.Downloader.Start();
			EnqueueDownloadItems();
		}

		OnPropertyChanged(nameof(ToggleDownloadButtonText));
		OnPropertyChanged(nameof(IsDownloading));
	}

	public void OnClickRetryFailedDownloads() {
		var allExceptFailedFilter = new DownloadItemFilter {
			IncludeStatuses = new HashSet<DownloadStatus> {
				DownloadStatus.Enqueued,
				DownloadStatus.Downloading,
				DownloadStatus.Success
			}
		};

		state.Db.RemoveDownloadItems(allExceptFailedFilter, FilterRemovalMode.KeepMatching);

		if (IsDownloading) {
			EnqueueDownloadItems();
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
