using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DHT.Desktop.Common;
using DHT.Server;
using DHT.Server.Data.Filters;
using DHT.Server.Data.Settings;
using DHT.Utils.Logging;
using DHT.Utils.Tasks;
using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Main.Controls;

sealed partial class DownloadItemFilterPanelModel : IAsyncDisposable {
	private static readonly Log Log = Log.ForType<DownloadItemFilterPanelModel>();
	
	public sealed record Unit(string Name, uint Scale);
	
	private static readonly Unit[] AllUnits = [
		new Unit("B", Scale: 1),
		new Unit("kB", Scale: 1024),
		new Unit("MB", Scale: 1024 * 1024)
	];
	
	private static readonly HashSet<string> FilterProperties = [
		nameof(LimitSize),
		nameof(MaximumSize),
		nameof(MaximumSizeUnit),
	];
	
	[Notify(Setter.Private)]
	private string filterStatisticsText = "";
	
	[Notify]
	private bool limitSize = false;
	
	[Notify]
	private ulong maximumSize = 0UL;
	
	[Notify]
	private Unit maximumSizeUnit = AllUnits[0];
	
	public IEnumerable<Unit> Units => AllUnits;
	
	private readonly State state;
	private readonly string verb;
	
	private readonly ThrottledTask<FilterSettings> saveFilterSettingsTask;
	private bool isLoadingFilterSettings;
	
	private readonly RestartableTask<long> downloadItemCountTask;
	private long? matchingItemCount;
	
	private readonly IDisposable downloadItemCountSubscription;
	private long? totalItemCount;
	
	[Obsolete("Designer")]
	public DownloadItemFilterPanelModel() : this(State.Dummy) {}
	
	public DownloadItemFilterPanelModel(State state, string verb = "Matches") {
		this.state = state;
		this.verb = verb;
		
		this.saveFilterSettingsTask = new ThrottledTask<FilterSettings>(Log, SaveFilterSettings, TimeSpan.FromSeconds(5), TaskScheduler.Default);
		
		this.downloadItemCountTask = new RestartableTask<long>(SetMatchingCount, TaskScheduler.FromCurrentSynchronizationContext());
		this.downloadItemCountSubscription = state.Db.Downloads.TotalCount.SubscribeLastOnUI(OnDownloadItemCountChanged, TimeSpan.FromMilliseconds(15));
		
		UpdateFilterStatistics();
		
		PropertyChanged += OnPropertyChanged;
	}
	
	public async Task Initialize() {
		isLoadingFilterSettings = true;
		
		LimitSize = await state.Db.Settings.Get(SettingsKey.DownloadsLimitSize, LimitSize);
		MaximumSize = await state.Db.Settings.Get(SettingsKey.DownloadsMaximumSize, MaximumSize);
		
		if (await state.Db.Settings.Get(SettingsKey.DownloadsMaximumSizeUnit, defaultValue: null) is {} unitName && AllUnits.FirstOrDefault(unit => unit.Name == unitName) is {} unitValue) {
			MaximumSizeUnit = unitValue;
		}
		
		isLoadingFilterSettings = false;
	}
	
	public async ValueTask DisposeAsync() {
		saveFilterSettingsTask.Dispose();
		
		downloadItemCountTask.Cancel();
		downloadItemCountSubscription.Dispose();
		
		await SaveFilterSettings(new FilterSettings(this));
	}
	
	private sealed record FilterSettings(bool LimitSize, ulong MaximumSize, Unit MaximumSizeUnit) {
		public FilterSettings(DownloadItemFilterPanelModel model) : this(model.LimitSize, model.MaximumSize, model.MaximumSizeUnit) {}
	}
	
	private async Task SaveFilterSettings(FilterSettings settings) {
		try {
			await state.Db.Settings.Set(async setter => {
				await setter.Set(SettingsKey.DownloadsLimitSize, settings.LimitSize);
				await setter.Set(SettingsKey.DownloadsMaximumSize, settings.MaximumSize);
				await setter.Set(SettingsKey.DownloadsMaximumSizeUnit, settings.MaximumSizeUnit.Name);
			});
		} catch (Exception e) {
			Log.Error("Could not save download filter settings.", e);
		}
	}
	
	private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName != null && FilterProperties.Contains(e.PropertyName)) {
			if (!isLoadingFilterSettings) {
				saveFilterSettingsTask.Post(new FilterSettings(this));
			}
			
			UpdateFilterStatistics();
		}
	}
	
	private void OnDownloadItemCountChanged(long newItemCount) {
		totalItemCount = newItemCount;
		UpdateFilterStatistics();
	}
	
	
	private void UpdateFilterStatistics() {
		DownloadItemFilter filter = CreateFilter();
		if (filter.IsEmpty) {
			downloadItemCountTask.Cancel();
			matchingItemCount = totalItemCount;
			UpdateFilterStatisticsText();
		}
		else {
			matchingItemCount = null;
			UpdateFilterStatisticsText();
			downloadItemCountTask.Restart(cancellationToken => state.Db.Downloads.Count(filter, cancellationToken));
		}
	}
	
	private void SetMatchingCount(long matchingAttachmentCount) {
		this.matchingItemCount = matchingAttachmentCount;
		UpdateFilterStatisticsText();
	}
	
	private void UpdateFilterStatisticsText() {
		string matchingItemCountStr = matchingItemCount?.Format() ?? "(...)";
		string totalItemCountStr = totalItemCount?.Format() ?? "(...)";
		FilterStatisticsText = verb + " " + matchingItemCountStr + " out of " + totalItemCountStr + " file" + (totalItemCount is null or 1 ? "." : "s.");
	}
	
	public DownloadItemFilter CreateFilter() {
		DownloadItemFilter filter = new ();
		
		if (LimitSize) {
			try {
				filter.MaxBytes = maximumSize * maximumSizeUnit.Scale;
			} catch (ArithmeticException) {
				// set no size limit, because the overflown size is larger than any file could possibly be
			}
		}
		
		return filter;
	}
}
