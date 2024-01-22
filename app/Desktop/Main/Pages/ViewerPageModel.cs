using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Desktop.Main.Controls;
using DHT.Desktop.Server;
using DHT.Server;
using DHT.Server.Data.Filters;
using DHT.Server.Service.Viewer;

namespace DHT.Desktop.Main.Pages;

sealed partial class ViewerPageModel : ObservableObject, IDisposable {
	public bool DatabaseToolFilterModeKeep { get; set; } = true;
	public bool DatabaseToolFilterModeRemove { get; set; } = false;

	[ObservableProperty]
	private bool hasFilters = false;

	public MessageFilterPanelModel FilterModel { get; }

	private readonly Window window;
	private readonly State state;

	[Obsolete("Designer")]
	public ViewerPageModel() : this(null!, State.Dummy) {}

	public ViewerPageModel(Window window, State state) {
		this.window = window;
		this.state = state;

		FilterModel = new MessageFilterPanelModel(window, state, "Will export");
		FilterModel.FilterPropertyChanged += OnFilterPropertyChanged;
	}

	public void Dispose() {
		FilterModel.Dispose();
	}

	private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e) {
		HasFilters = FilterModel.HasAnyFilters;
	}

	public async void OnClickOpenViewer() {
		try {
			string serverUrl = "http://127.0.0.1:" + ServerConfiguration.Port;
			string serverToken = ServerConfiguration.Token;
			string sessionId = state.ViewerSessions.Register(new ViewerSession(FilterModel.CreateFilter())).ToString();
			
			Process.Start(new ProcessStartInfo(serverUrl + "/viewer/?token=" + HttpUtility.UrlEncode(serverToken) + "&session=" + HttpUtility.UrlEncode(sessionId)) {
				UseShellExecute = true
			});
		} catch (Exception e) {
			await Dialog.ShowOk(window, "Open Viewer", "Could not open viewer: " + e.Message);
		}
	}

	public async Task OnClickApplyFiltersToDatabase() {
		var filter = FilterModel.CreateFilter();
		var messageCount = await ProgressDialog.ShowIndeterminate(window, "Apply Filters", "Counting matching messages...", _ => state.Db.Messages.Count(filter));

		if (DatabaseToolFilterModeKeep) {
			if (DialogResult.YesNo.Yes == await Dialog.ShowYesNo(window, "Keep Matching Messages in This Database", messageCount.Pluralize("message") + " will be kept, and the rest will be removed from this database. This action cannot be undone. Proceed?")) {
				await ApplyFilterToDatabase(filter, FilterRemovalMode.KeepMatching);
			}
		}
		else if (DatabaseToolFilterModeRemove) {
			if (DialogResult.YesNo.Yes == await Dialog.ShowYesNo(window, "Remove Matching Messages in This Database", messageCount.Pluralize("message") + " will be removed from this database. This action cannot be undone. Proceed?")) {
				await ApplyFilterToDatabase(filter, FilterRemovalMode.RemoveMatching);
			}
		}
	}

	private async Task ApplyFilterToDatabase(MessageFilter filter, FilterRemovalMode removalMode) {
		await ProgressDialog.ShowIndeterminate(window, "Apply Filters", "Removing messages...", _ => state.Db.Messages.Remove(filter, removalMode));
	}
}
