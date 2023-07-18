using System;
using System.Collections.Generic;
using System.ComponentModel;
using DHT.Desktop.Common;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Utils.Models;
using DHT.Utils.Tasks;

namespace DHT.Desktop.Main.Controls;

sealed class AttachmentFilterPanelModel : BaseModel, IDisposable {
	public sealed record Unit(string Name, uint Scale);

	private static readonly Unit[] AllUnits = {
		new ("B", 1),
		new ("kB", 1024),
		new ("MB", 1024 * 1024)
	};

	private static readonly HashSet<string> FilterProperties = new () {
		nameof(LimitSize),
		nameof(MaximumSize),
		nameof(MaximumSizeUnit)
	};

	public string FilterStatisticsText { get; private set; } = "";

	private bool limitSize = false;
	private ulong maximumSize = 0L;
	private Unit maximumSizeUnit = AllUnits[0];

	public bool LimitSize {
		get => limitSize;
		set => Change(ref limitSize, value);
	}

	public ulong MaximumSize {
		get => maximumSize;
		set => Change(ref maximumSize, value);
	}

	public Unit MaximumSizeUnit {
		get => maximumSizeUnit;
		set => Change(ref maximumSizeUnit, value);
	}

	public IEnumerable<Unit> Units => AllUnits;

	private readonly IDatabaseFile db;
	private readonly string verb;

	private readonly AsyncValueComputer<long> matchingAttachmentCountComputer;
	private long? matchingAttachmentCount;
	private long? totalAttachmentCount;

	[Obsolete("Designer")]
	public AttachmentFilterPanelModel() : this(DummyDatabaseFile.Instance) {}

	public AttachmentFilterPanelModel(IDatabaseFile db, string verb = "Matches") {
		this.db = db;
		this.verb = verb;

		this.matchingAttachmentCountComputer = AsyncValueComputer<long>.WithResultProcessor(SetAttachmentCounts).Build();

		UpdateFilterStatistics();

		PropertyChanged += OnPropertyChanged;
		db.Statistics.PropertyChanged += OnDbStatisticsChanged;
	}

	public void Dispose() {
		db.Statistics.PropertyChanged -= OnDbStatisticsChanged;
	}

	private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName != null && FilterProperties.Contains(e.PropertyName)) {
			UpdateFilterStatistics();
		}
	}

	private void OnDbStatisticsChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName == nameof(DatabaseStatistics.TotalAttachments)) {
			totalAttachmentCount = db.Statistics.TotalAttachments;
			UpdateFilterStatistics();
		}
	}

	private void UpdateFilterStatistics() {
		var filter = CreateFilter();
		if (filter.IsEmpty) {
			matchingAttachmentCountComputer.Cancel();
			matchingAttachmentCount = totalAttachmentCount;
			UpdateFilterStatisticsText();
		}
		else {
			matchingAttachmentCount = null;
			UpdateFilterStatisticsText();
			matchingAttachmentCountComputer.Compute(() => db.CountAttachments(filter));
		}
	}

	private void SetAttachmentCounts(long matchingAttachmentCount) {
		this.matchingAttachmentCount = matchingAttachmentCount;
		UpdateFilterStatisticsText();
	}

	private void UpdateFilterStatisticsText() {
		var matchingAttachmentCountStr = matchingAttachmentCount?.Format() ?? "(...)";
		var totalAttachmentCountStr = totalAttachmentCount?.Format() ?? "(...)";

		FilterStatisticsText = verb + " " + matchingAttachmentCountStr + " out of " + totalAttachmentCountStr + " attachment" + (totalAttachmentCount is null or 1 ? "." : "s.");
		OnPropertyChanged(nameof(FilterStatisticsText));
	}

	public AttachmentFilter CreateFilter() {
		AttachmentFilter filter = new();

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
