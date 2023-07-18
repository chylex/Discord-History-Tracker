using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.CheckBox;
using DHT.Desktop.Dialogs.Message;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Utils.Models;
using DHT.Utils.Tasks;

namespace DHT.Desktop.Main.Controls;

sealed class MessageFilterPanelModel : BaseModel, IDisposable {
	private static readonly HashSet<string> FilterProperties = new () {
		nameof(FilterByDate),
		nameof(StartDate),
		nameof(EndDate),
		nameof(FilterByChannel),
		nameof(IncludedChannels),
		nameof(FilterByUser),
		nameof(IncludedUsers)
	};

	public string FilterStatisticsText { get; private set; } = "";

	public event PropertyChangedEventHandler? FilterPropertyChanged;

	public bool HasAnyFilters => FilterByDate || FilterByChannel || FilterByUser;

	private bool filterByDate = false;
	private DateTime? startDate = null;
	private DateTime? endDate = null;
	private bool filterByChannel = false;
	private HashSet<ulong>? includedChannels = null;
	private bool filterByUser = false;
	private HashSet<ulong>? includedUsers = null;

	public bool FilterByDate {
		get => filterByDate;
		set => Change(ref filterByDate, value);
	}

	public DateTime? StartDate {
		get => startDate;
		set => Change(ref startDate, value);
	}

	public DateTime? EndDate {
		get => endDate;
		set => Change(ref endDate, value);
	}

	public bool FilterByChannel {
		get => filterByChannel;
		set => Change(ref filterByChannel, value);
	}

	public HashSet<ulong> IncludedChannels {
		get => includedChannels ?? db.GetAllChannels().Select(static channel => channel.Id).ToHashSet();
		set => Change(ref includedChannels, value);
	}

	public bool FilterByUser {
		get => filterByUser;
		set => Change(ref filterByUser, value);
	}

	public HashSet<ulong> IncludedUsers {
		get => includedUsers ?? db.GetAllUsers().Select(static user => user.Id).ToHashSet();
		set => Change(ref includedUsers, value);
	}

	private string channelFilterLabel = "";

	public string ChannelFilterLabel {
		get => channelFilterLabel;
		set => Change(ref channelFilterLabel, value);
	}

	private string userFilterLabel = "";

	public string UserFilterLabel {
		get => userFilterLabel;
		set => Change(ref userFilterLabel, value);
	}

	private readonly Window window;
	private readonly IDatabaseFile db;
	private readonly string verb;

	private readonly AsyncValueComputer<long> exportedMessageCountComputer;
	private long? exportedMessageCount;
	private long? totalMessageCount;

	[Obsolete("Designer")]
	public MessageFilterPanelModel() : this(null!, DummyDatabaseFile.Instance) {}

	public MessageFilterPanelModel(Window window, IDatabaseFile db, string verb = "Matches") {
		this.window = window;
		this.db = db;
		this.verb = verb;

		this.exportedMessageCountComputer = AsyncValueComputer<long>.WithResultProcessor(SetExportedMessageCount).Build();

		UpdateFilterStatistics();
		UpdateChannelFilterLabel();
		UpdateUserFilterLabel();

		PropertyChanged += OnPropertyChanged;
		db.Statistics.PropertyChanged += OnDbStatisticsChanged;
	}

	public void Dispose() {
		db.Statistics.PropertyChanged -= OnDbStatisticsChanged;
	}

	private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName != null && FilterProperties.Contains(e.PropertyName)) {
			UpdateFilterStatistics();
			FilterPropertyChanged?.Invoke(sender, e);
		}

		if (e.PropertyName is nameof(FilterByChannel) or nameof(IncludedChannels)) {
			UpdateChannelFilterLabel();
		}
		else if (e.PropertyName is nameof(FilterByUser) or nameof(IncludedUsers)) {
			UpdateUserFilterLabel();
		}
	}

	private void OnDbStatisticsChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName == nameof(DatabaseStatistics.TotalMessages)) {
			totalMessageCount = db.Statistics.TotalMessages;
			UpdateFilterStatistics();
		}
		else if (e.PropertyName == nameof(DatabaseStatistics.TotalChannels)) {
			UpdateChannelFilterLabel();
		}
		else if (e.PropertyName == nameof(DatabaseStatistics.TotalUsers)) {
			UpdateUserFilterLabel();
		}
	}

	private void UpdateFilterStatistics() {
		var filter = CreateFilter();
		if (filter.IsEmpty) {
			exportedMessageCountComputer.Cancel();
			exportedMessageCount = totalMessageCount;
			UpdateFilterStatisticsText();
		}
		else {
			exportedMessageCount = null;
			UpdateFilterStatisticsText();
			exportedMessageCountComputer.Compute(() => db.CountMessages(filter));
		}
	}

	private void SetExportedMessageCount(long exportedMessageCount) {
		this.exportedMessageCount = exportedMessageCount;
		UpdateFilterStatisticsText();
	}

	private void UpdateFilterStatisticsText() {
		var exportedMessageCountStr = exportedMessageCount?.Format() ?? "(...)";
		var totalMessageCountStr = totalMessageCount?.Format() ?? "(...)";

		FilterStatisticsText = verb + " " + exportedMessageCountStr + " out of " + totalMessageCountStr + " message" + (totalMessageCount is null or 1 ? "." : "s.");
		OnPropertyChanged(nameof(FilterStatisticsText));
	}

	public async void OpenChannelFilterDialog() {
		var servers = db.GetAllServers().ToDictionary(static server => server.Id);
		var items = new List<CheckBoxItem<ulong>>();
		var included = IncludedChannels;

		foreach (var channel in db.GetAllChannels()) {
			var channelId = channel.Id;
			var channelName = channel.Name;

			string title;
			if (servers.TryGetValue(channel.Server, out var server)) {
				var titleBuilder = new StringBuilder();
				var serverType = server.Type;

				titleBuilder.Append('[')
				            .Append(ServerTypes.ToString(serverType))
				            .Append("] ");

				if (serverType == ServerType.DirectMessage) {
					titleBuilder.Append(channelName);
				}
				else {
					titleBuilder.Append(server.Name)
					            .Append(" - ")
					            .Append(channelName);
				}

				title = titleBuilder.ToString();
			}
			else {
				title = channelName;
			}

			items.Add(new CheckBoxItem<ulong>(channelId) {
				Title = title,
				Checked = included.Contains(channelId)
			});
		}

		var result = await OpenIdFilterDialog(window, "Included Channels", items);
		if (result != null) {
			IncludedChannels = result;
		}
	}

	public async void OpenUserFilterDialog() {
		var items = new List<CheckBoxItem<ulong>>();
		var included = IncludedUsers;

		foreach (var user in db.GetAllUsers()) {
			var name = user.Name;
			var discriminator = user.Discriminator;

			items.Add(new CheckBoxItem<ulong>(user.Id) {
				Title = discriminator == null ? name : name + " #" + discriminator,
				Checked = included.Contains(user.Id)
			});
		}

		var result = await OpenIdFilterDialog(window, "Included Users", items);
		if (result != null) {
			IncludedUsers = result;
		}
	}

	private void UpdateChannelFilterLabel() {
		long total = db.Statistics.TotalChannels;
		long included = FilterByChannel ? IncludedChannels.Count : total;
		ChannelFilterLabel = "Selected " + included.Format() + " / " + total.Pluralize("channel") + ".";
	}

	private void UpdateUserFilterLabel() {
		long total = db.Statistics.TotalUsers;
		long included = FilterByUser ? IncludedUsers.Count : total;
		UserFilterLabel = "Selected " + included.Format() + " / " + total.Pluralize("user") + ".";
	}

	public MessageFilter CreateFilter() {
		MessageFilter filter = new();

		if (FilterByDate) {
			filter.StartDate = StartDate;
			filter.EndDate = EndDate?.AddDays(1).AddMilliseconds(-1);
		}

		if (FilterByChannel) {
			filter.ChannelIds = new HashSet<ulong>(IncludedChannels);
		}

		if (FilterByUser) {
			filter.UserIds = new HashSet<ulong>(IncludedUsers);
		}

		return filter;
	}

	private static async Task<HashSet<ulong>?> OpenIdFilterDialog(Window window, string title, List<CheckBoxItem<ulong>> items) {
		items.Sort(static (item1, item2) => item1.Title.CompareTo(item2.Title));

		var model = new CheckBoxDialogModel<ulong>(items) {
			Title = title
		};

		var dialog = new CheckBoxDialog { DataContext = model };
		var result = await dialog.ShowDialog<DialogResult.OkCancel>(window);

		return result == DialogResult.OkCancel.Ok ? model.SelectedItems.Select(static item => item.Item).ToHashSet() : null;
	}
}
