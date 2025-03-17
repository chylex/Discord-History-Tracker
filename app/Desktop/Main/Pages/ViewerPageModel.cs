using System;
using System.ComponentModel;
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
using DHT.Utils.Logging;

namespace DHT.Desktop.Main.Pages;

sealed partial class ViewerPageModel : ObservableObject, IDisposable {
	private static readonly Log Log = Log.ForType<ViewerPageModel>();
	
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
			SystemUtils.OpenUrl(serverUrl + "/viewer/?token=" + HttpUtility.UrlEncode(serverToken) + "&session=" + HttpUtility.UrlEncode(sessionId));
		} catch (Exception e) {
			await Dialog.ShowOk(window, "Open Viewer", "Could not open viewer: " + e.Message);
		}
	}
	
	public async Task OnClickApplyFiltersToDatabase() {
		try {
			MessageFilter filter = FilterModel.CreateFilter();
			long messageCount = await ProgressDialog.ShowIndeterminate(window, "Apply Filters", "Counting matching messages...", _ => state.Db.Messages.Count(filter));
			
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
		} catch (Exception e) {
			Log.Error(e);
			await Dialog.ShowOk(window, "Apply Filters", "Could not apply filters: " + e.Message);
		}
	}
	
	private async Task ApplyFilterToDatabase(MessageFilter filter, FilterRemovalMode removalMode) {
		await ProgressDialog.Show(window, "Apply Filters", async (_, callback) => {
			await callback.UpdateIndeterminate("Removing messages...");
			Log.Info("Removed messages: " + await state.Db.Messages.Remove(filter, removalMode));
			
			await callback.UpdateIndeterminate("Cleaning up attachments...");
			Log.Info("Removed orphaned attachments: " + await state.Db.Messages.RemoveUnreachableAttachments());
			
			await callback.UpdateIndeterminate("Cleaning up users...");
			Log.Info("Removed orphaned users: " + await state.Db.Users.RemoveUnreachable());
			
			await callback.UpdateIndeterminate("Cleaning up channels...");
			Log.Info("Removed orphaned channels: " + await state.Db.Channels.RemoveUnreachable());
			
			await callback.UpdateIndeterminate("Cleaning up servers...");
			Log.Info("Removed orphaned servers: " + await state.Db.Servers.RemoveUnreachable());
		});
	}
}
