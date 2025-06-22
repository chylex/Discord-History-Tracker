using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.CheckBox;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Server;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Utils.Tasks;
using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Main.Controls;

sealed partial class MessageFilterPanelModel : IDisposable {
	private static readonly HashSet<string> FilterProperties = [
		nameof(FilterByDate),
		nameof(StartDate),
		nameof(EndDate),
		nameof(FilterByChannel),
		nameof(IncludedChannels),
		nameof(FilterByUser),
		nameof(IncludedUsers),
	];
	
	public event PropertyChangedEventHandler? FilterPropertyChanged;
	
	public bool HasAnyFilters => FilterByDate || FilterByChannel || FilterByUser;
	
	[Notify]
	private string filterStatisticsText = "";
	
	[Notify]
	private bool filterByDate = false;
	
	[Notify]
	private DateTime? startDate = null;
	
	[Notify]
	private DateTime? endDate = null;
	
	[Notify]
	private bool filterByChannel = false;
	
	[Notify]
	private HashSet<ulong>? includedChannels = null;
	
	[Notify]
	private bool filterByUser = false;
	
	[Notify]
	private HashSet<ulong>? includedUsers = null;
	
	[Notify]
	private string channelFilterLabel = "";
	
	[Notify]
	private string userFilterLabel = "";
	
	private readonly Window window;
	private readonly State state;
	private readonly string verb;
	
	private readonly RestartableTask<long> exportedMessageCountTask;
	private long? exportedMessageCount;
	
	private readonly IDisposable messageCountSubscription;
	private long? totalMessageCount;
	
	private readonly IDisposable channelCountSubscription;
	private long? totalChannelCount;
	
	private readonly IDisposable userCountSubscription;
	private long? totalUserCount;
	
	[Obsolete("Designer")]
	public MessageFilterPanelModel() : this(null!, State.Dummy) {}
	
	public MessageFilterPanelModel(Window window, State state, string verb = "Matches") {
		this.window = window;
		this.state = state;
		this.verb = verb;
		
		this.exportedMessageCountTask = new RestartableTask<long>(SetExportedMessageCount, TaskScheduler.FromCurrentSynchronizationContext());
		
		this.messageCountSubscription = state.Db.Messages.TotalCount.ObserveOn(AvaloniaScheduler.Instance).Subscribe(OnMessageCountChanged);
		this.channelCountSubscription = state.Db.Channels.TotalCount.ObserveOn(AvaloniaScheduler.Instance).Subscribe(OnChannelCountChanged);
		this.userCountSubscription = state.Db.Users.TotalCount.ObserveOn(AvaloniaScheduler.Instance).Subscribe(OnUserCountChanged);
		
		UpdateFilterStatistics();
		UpdateChannelFilterLabel();
		UpdateUserFilterLabel();
		
		PropertyChanged += OnPropertyChanged;
	}
	
	public void Dispose() {
		exportedMessageCountTask.Cancel();
		
		messageCountSubscription.Dispose();
		channelCountSubscription.Dispose();
		userCountSubscription.Dispose();
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
	
	private void OnMessageCountChanged(long newMessageCount) {
		totalMessageCount = newMessageCount;
		UpdateFilterStatistics();
	}
	
	private void OnChannelCountChanged(long newChannelCount) {
		totalChannelCount = newChannelCount;
		UpdateChannelFilterLabel();
	}
	
	private void OnUserCountChanged(long newUserCount) {
		totalUserCount = newUserCount;
		UpdateUserFilterLabel();
	}
	
	private void UpdateChannelFilterLabel() {
		if (totalChannelCount.HasValue) {
			long total = totalChannelCount.Value;
			long included = FilterByChannel && IncludedChannels != null ? IncludedChannels.Count : total;
			ChannelFilterLabel = "Selected " + included.Format() + " / " + total.Pluralize("channel") + ".";
		}
		else {
			ChannelFilterLabel = "Loading...";
		}
	}
	
	private void UpdateUserFilterLabel() {
		if (totalUserCount.HasValue) {
			long total = totalUserCount.Value;
			long included = FilterByUser && IncludedUsers != null ? IncludedUsers.Count : total;
			UserFilterLabel = "Selected " + included.Format() + " / " + total.Pluralize("user") + ".";
		}
		else {
			UserFilterLabel = "Loading...";
		}
	}
	
	private void UpdateFilterStatistics() {
		MessageFilter filter = CreateFilter();
		if (filter.IsEmpty) {
			exportedMessageCountTask.Cancel();
			exportedMessageCount = totalMessageCount;
			UpdateFilterStatisticsText();
		}
		else {
			exportedMessageCount = null;
			UpdateFilterStatisticsText();
			exportedMessageCountTask.Restart(cancellationToken => state.Db.Messages.Count(filter, cancellationToken));
		}
	}
	
	private void SetExportedMessageCount(long exportedMessageCount) {
		this.exportedMessageCount = exportedMessageCount;
		UpdateFilterStatisticsText();
	}
	
	private void UpdateFilterStatisticsText() {
		string exportedMessageCountStr = exportedMessageCount?.Format() ?? "(...)";
		string totalMessageCountStr = totalMessageCount?.Format() ?? "(...)";
		FilterStatisticsText = verb + " " + exportedMessageCountStr + " out of " + totalMessageCountStr + " message" + (totalMessageCount is null or 1 ? "." : "s.");
	}
	
	[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
	private readonly record struct ChannelFilterKey(byte Type, ulong? ServerId, string Title) : IComparable<ChannelFilterKey> {
		public static ChannelFilterKey DirectMessages { get; } = new (Type: 1, ServerId: null, Title: "Direct Messages");
		public static ChannelFilterKey GroupMessages { get; } = new (Type: 2, ServerId: null, Title: "Group Messages");
		public static ChannelFilterKey Unknown { get; } = new (Type: 4, ServerId: null, Title: "Unknown");
		
		public static ChannelFilterKey For(DHT.Server.Data.Server server) {
			return server.Type switch {
				ServerType.Server        => new ChannelFilterKey(Type: 3, server.Id, "Server - " + server.Name),
				ServerType.Group         => GroupMessages,
				ServerType.DirectMessage => DirectMessages,
				_                        => Unknown,
			};
		}
		
		public bool Equals(ChannelFilterKey other) {
			return Type == other.Type && ServerId == other.ServerId;
		}
		
		public override int GetHashCode() {
			return HashCode.Combine(Type, ServerId);
		}
		
		public int CompareTo(ChannelFilterKey other) {
			int result = Type.CompareTo(other.Type);
			if (result != 0) {
				return result;
			}
			else {
				return Title.CompareTo(other.Title);
			}
		}
	}
	
	public async Task OpenChannelFilterDialog() {
		async Task<ImmutableArray<ICheckBoxItem>> PrepareChannelItems(ProgressDialog dialog) {
			CheckBoxItemList<ChannelFilterKey, ulong> items = new CheckBoxItemList<ChannelFilterKey, ulong>();
			Dictionary<ulong, DHT.Server.Data.Server> servers = await state.Db.Servers.Get().ToDictionaryAsync(static server => server.Id);
			
			foreach (ChannelFilterKey channelFilterKey in servers.Values.Select(ChannelFilterKey.For).Order()) {
				items.AddParent(channelFilterKey, channelFilterKey.Title);
			}
			
			await foreach (Channel channel in state.Db.Channels.Get().OrderBy(static channel => channel.Position ?? int.MinValue).ThenBy(static channel => channel.Name)) {
				ChannelFilterKey key = servers.TryGetValue(channel.Server, out var server)
					                       ? ChannelFilterKey.For(server)
					                       : ChannelFilterKey.Unknown;
				
				items.Add(key, channel.Id, channel.Name, isChecked: IncludedChannels == null || IncludedChannels.Contains(channel.Id));
			}
			
			return items.ToCheckBoxItems();
		}
		
		const string Title = "Included Channels";
		
		ImmutableArray<ICheckBoxItem> items;
		try {
			items = await ProgressDialog.ShowIndeterminate(window, Title, "Loading channels...", PrepareChannelItems);
		} catch (Exception e) {
			await Dialog.ShowOk(window, Title, "Error loading channels: " + e.Message);
			return;
		}
		
		HashSet<ulong>? result = await OpenIdFilterDialog(Title, items);
		if (result != null) {
			IncludedChannels = result;
		}
	}
	
	public async Task OpenUserFilterDialog() {
		async Task<ImmutableArray<ICheckBoxItem>> PrepareUserItems(ProgressDialog dialog) {
			CheckBoxItemList<ulong, ulong> items = new CheckBoxItemList<ulong, ulong>();
			
			static string GetDisplayName(User user) {
				return user.DisplayName == null ? user.Name : $"{user.DisplayName} ({user.Name})";
			}
			
			await foreach ((ulong id, string name) in state.Db.Users.Get().Select(static user => (user.Id, GetDisplayName(user))).OrderBy(static pair => pair.Item2)) {
				items.Add(
					value: id,
					title: name,
					isChecked: IncludedUsers == null || IncludedUsers.Contains(id)
				);
			}
			
			return items.ToCheckBoxItems();
		}
		
		const string Title = "Included Users";
		
		ImmutableArray<ICheckBoxItem> items;
		try {
			items = await ProgressDialog.ShowIndeterminate(window, Title, "Loading users...", PrepareUserItems);
		} catch (Exception e) {
			await Dialog.ShowOk(window, Title, "Error loading users: " + e.Message);
			return;
		}
		
		HashSet<ulong>? result = await OpenIdFilterDialog(Title, items);
		if (result != null) {
			IncludedUsers = result;
		}
	}
	
	private async Task<HashSet<ulong>?> OpenIdFilterDialog(string title, ImmutableArray<ICheckBoxItem> items) {
		var model = new CheckBoxDialogModel<ulong>(items) {
			Title = title,
		};
		
		var dialog = new CheckBoxDialog { DataContext = model };
		var result = await dialog.ShowDialog<DialogResult.OkCancel>(window);
		
		return result == DialogResult.OkCancel.Ok ? model.SelectedValues.ToHashSet() : null;
	}
	
	public MessageFilter CreateFilter() {
		MessageFilter filter = new ();
		
		if (FilterByDate) {
			filter.StartDate = StartDate;
			filter.EndDate = EndDate?.AddDays(1).AddMilliseconds(-1);
		}
		
		if (FilterByChannel && IncludedChannels != null) {
			filter.ChannelIds = new HashSet<ulong>(IncludedChannels);
		}
		
		if (FilterByUser && IncludedUsers != null) {
			filter.UserIds = new HashSet<ulong>(IncludedUsers);
		}
		
		return filter;
	}
}
