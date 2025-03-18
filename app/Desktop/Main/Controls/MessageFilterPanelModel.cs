using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
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
	
	public async Task OpenChannelFilterDialog() {
		async Task<List<CheckBoxItem<ulong>>> PrepareChannelItems(ProgressDialog dialog) {
			var items = new List<CheckBoxItem<ulong>>();
			Dictionary<ulong, DHT.Server.Data.Server> servers = await state.Db.Servers.Get().ToDictionaryAsync(static server => server.Id);
			
			await foreach (Channel channel in state.Db.Channels.Get()) {
				ulong channelId = channel.Id;
				string channelName = channel.Name;
				
				string title;
				if (servers.TryGetValue(channel.Server, out var server)) {
					var titleBuilder = new StringBuilder();
					ServerType? serverType = server.Type;
					
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
					IsChecked = IncludedChannels == null || IncludedChannels.Contains(channelId),
				});
			}
			
			return items;
		}
		
		const string Title = "Included Channels";
		
		List<CheckBoxItem<ulong>> items;
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
		async Task<List<CheckBoxItem<ulong>>> PrepareUserItems(ProgressDialog dialog) {
			var checkBoxItems = new List<CheckBoxItem<ulong>>();
			
			await foreach (User user in state.Db.Users.Get()) {
				checkBoxItems.Add(new CheckBoxItem<ulong>(user.Id) {
					Title = user.DisplayName == null ? user.Name : $"{user.DisplayName} ({user.Name})",
					IsChecked = IncludedUsers == null || IncludedUsers.Contains(user.Id),
				});
			}
			
			return checkBoxItems;
		}
		
		const string Title = "Included Users";
		
		List<CheckBoxItem<ulong>> items;
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
	
	private async Task<HashSet<ulong>?> OpenIdFilterDialog(string title, List<CheckBoxItem<ulong>> items) {
		items.Sort(static (item1, item2) => item1.Title.CompareTo(item2.Title));
		
		var model = new CheckBoxDialogModel<ulong>(items) {
			Title = title,
		};
		
		var dialog = new CheckBoxDialog { DataContext = model };
		var result = await dialog.ShowDialog<DialogResult.OkCancel>(window);
		
		return result == DialogResult.OkCancel.Ok ? model.SelectedItems.Select(static item => item.Item).ToHashSet() : null;
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
