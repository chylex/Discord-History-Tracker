using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Avalonia.Threading;
using DHT.Desktop.Common;
using DHT.Desktop.Main.Controls;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Server.Download;
using DHT.Utils.Models;
using DHT.Utils.Tasks;

namespace DHT.Desktop.Main.Pages;

sealed class AttachmentsPageModel : BaseModel, IDisposable {
	private static readonly DownloadItemFilter EnqueuedItemFilter = new() {
		IncludeStatuses = new HashSet<DownloadStatus> {
			DownloadStatus.Enqueued
		}
	};

	private bool isThreadDownloadButtonEnabled = true;

	public string ToggleDownloadButtonText => downloadThread == null ? "Start Downloading" : "Stop Downloading";

	public bool IsToggleDownloadButtonEnabled {
		get => isThreadDownloadButtonEnabled;
		set => Change(ref isThreadDownloadButtonEnabled, value);
	}

	public string DownloadMessage { get; set; } = "";
	public double DownloadProgress => allItemsCount is null or 0 ? 0.0 : 100.0 * doneItemsCount / allItemsCount.Value;

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

	public bool IsDownloading => downloadThread != null;
	public bool HasFailedDownloads => statisticsFailed.Items > 0;

	private readonly IDatabaseFile db;
	private readonly AsyncValueComputer<DownloadStatusStatistics>.Single downloadStatisticsComputer;
	private BackgroundDownloadThread? downloadThread;

	private int doneItemsCount;
	private int? allItemsCount;

	public AttachmentsPageModel() : this(DummyDatabaseFile.Instance) {}

	public AttachmentsPageModel(IDatabaseFile db) {
		this.db = db;
		this.FilterModel = new AttachmentFilterPanelModel(db);

		this.downloadStatisticsComputer = AsyncValueComputer<DownloadStatusStatistics>.WithResultProcessor(UpdateStatistics).WithOutdatedResults().BuildWithComputer(db.GetDownloadStatusStatistics);
		this.downloadStatisticsComputer.Recompute();

		db.Statistics.PropertyChanged += OnDbStatisticsChanged;
	}

	public void Dispose() {
		db.Statistics.PropertyChanged -= OnDbStatisticsChanged;

		FilterModel.Dispose();
		DisposeDownloadThread();
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
		db.EnqueueDownloadItems(filter);

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

		allItemsCount = doneItemsCount + statisticsEnqueued.Items;
		UpdateDownloadMessage();
	}

	private void UpdateDownloadMessage() {
		DownloadMessage = IsDownloading ? doneItemsCount.Format() + " / " + (allItemsCount?.Format() ?? "?") : "";

		OnPropertyChanged(nameof(DownloadMessage));
		OnPropertyChanged(nameof(DownloadProgress));
	}

	private void DownloadThreadOnOnItemFinished(object? sender, DownloadItem e) {
		Interlocked.Increment(ref doneItemsCount);

		Dispatcher.UIThread.Invoke(UpdateDownloadMessage);
		downloadStatisticsComputer.Recompute();
	}

	private void DownloadThreadOnOnServerStopped(object? sender, EventArgs e) {
		downloadStatisticsComputer.Recompute();
		IsToggleDownloadButtonEnabled = true;
	}

	public void OnClickToggleDownload() {
		if (downloadThread == null) {
			EnqueueDownloadItems();
			downloadThread = new BackgroundDownloadThread(db);
			downloadThread.OnItemFinished += DownloadThreadOnOnItemFinished;
			downloadThread.OnServerStopped += DownloadThreadOnOnServerStopped;
		}
		else {
			IsToggleDownloadButtonEnabled = false;
			DisposeDownloadThread();

			db.RemoveDownloadItems(EnqueuedItemFilter, FilterRemovalMode.RemoveMatching);

			doneItemsCount = 0;
			allItemsCount = null;
			UpdateDownloadMessage();
		}

		OnPropertyChanged(nameof(ToggleDownloadButtonText));
		OnPropertyChanged(nameof(IsDownloading));
	}

	public void OnClickRetryFailedDownloads() {
		var allExceptFailedFilter = new DownloadItemFilter {
			IncludeStatuses = new HashSet<DownloadStatus> {
				DownloadStatus.Enqueued,
				DownloadStatus.Success
			}
		};

		db.RemoveDownloadItems(allExceptFailedFilter, FilterRemovalMode.KeepMatching);

		if (IsDownloading) {
			EnqueueDownloadItems();
		}
	}

	private void DisposeDownloadThread() {
		if (downloadThread != null) {
			downloadThread.OnItemFinished -= DownloadThreadOnOnItemFinished;
			downloadThread.StopThread();
		}

		downloadThread = null;
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
