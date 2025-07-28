using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Discord;
using DHT.Desktop.Server;
using DHT.Utils.Logging;
using PropertyChanged.SourceGenerator;
using static DHT.Desktop.Program;

namespace DHT.Desktop.Main.Pages;

sealed partial class TrackingPageModel {
	private static readonly Log Log = Log.ForType<TrackingPageModel>();
	
	[Notify(Setter.Private)]
	private bool? areDevToolsEnabled = null;
	
	[Notify(Setter.Private)]
	private bool isToggleAppDevToolsButtonEnabled = false;
	
	public string OpenDevToolsShortcutText { get; } = OperatingSystem.IsMacOS() ? "Cmd+Shift+I" : "Ctrl+Shift+I";
	
	[DependsOn(nameof(AreDevToolsEnabled), nameof(IsToggleAppDevToolsButtonEnabled))]
	public string ToggleAppDevToolsButtonText {
		get {
			if (!AreDevToolsEnabled.HasValue) {
				return "Loading...";
			}
			
			if (!IsToggleAppDevToolsButtonEnabled) {
				return "Unavailable";
			}
			
			return (AreDevToolsEnabled.Value ? "Disable" : "Enable") + " " + OpenDevToolsShortcutText;
		}
	}
	
	private readonly Window window;
	
	[Obsolete("Designer")]
	public TrackingPageModel() : this(null!) {}
	
	public TrackingPageModel(Window window) {
		this.window = window;
		
		Task.Factory.StartNew(InitializeDevToolsToggle, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
	}
	
	public async Task<bool> OnClickCopyTrackingScript() {
		string url = ServerConfiguration.HttpHost + $"/get-tracking-script?token={HttpUtility.UrlEncode(ServerConfiguration.Token)}";
		string script = (await Resources.ReadTextAsync("tracker-loader.js")).Trim().Replace("{url}", url);
		return await TryCopy(script, "Copy Tracking Script");
	}
	
	private async Task InitializeDevToolsToggle() {
		bool? devToolsEnabled = await Task.Run(DiscordAppSettings.AreDevToolsEnabled);
		
		if (devToolsEnabled.HasValue) {
			AreDevToolsEnabled = devToolsEnabled.Value;
			IsToggleAppDevToolsButtonEnabled = true;
		}
		else {
			IsToggleAppDevToolsButtonEnabled = false;
		}
	}
	
	public async Task OnClickToggleAppDevTools() {
		const string DialogTitle = "Discord App Settings File";
		
		if (!AreDevToolsEnabled.HasValue) {
			return;
		}
		
		bool oldState = AreDevToolsEnabled.Value;
		bool newState = !oldState;
		
		switch (await DiscordAppSettings.ConfigureDevTools(newState)) {
			case SettingsJsonResult.Success:
				AreDevToolsEnabled = newState;
				await Dialog.ShowOk(window, DialogTitle, OpenDevToolsShortcutText + " was " + (newState ? "enabled." : "disabled.") + " Restart the Discord app for the change to take effect.");
				break;
			
			case SettingsJsonResult.AlreadySet:
				await Dialog.ShowOk(window, DialogTitle, OpenDevToolsShortcutText + " is already " + (newState ? "enabled." : "disabled."));
				AreDevToolsEnabled = newState;
				break;
			
			case SettingsJsonResult.FileNotFound:
				await Dialog.ShowOk(window, DialogTitle, "Cannot find the settings file:\n" + DiscordAppSettings.JsonFilePath);
				break;
			
			case SettingsJsonResult.ReadError:
				await Dialog.ShowOk(window, DialogTitle, "Cannot read the settings file:\n" + DiscordAppSettings.JsonFilePath);
				break;
			
			case SettingsJsonResult.InvalidJson:
				await Dialog.ShowOk(window, DialogTitle, "Unknown format of the settings file:\n" + DiscordAppSettings.JsonFilePath);
				break;
			
			case SettingsJsonResult.WriteError:
				await Dialog.ShowOk(window, DialogTitle, "Cannot save the settings file:\n" + DiscordAppSettings.JsonFilePath);
				break;
			
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
	
	public async Task OnClickInstallOrUpdateUserscript() {
		try {
			SystemUtils.OpenUrl(ServerConfiguration.HttpHost + "/get-userscript/dht.user.js");
		} catch (Exception e) {
			await Dialog.ShowOk(window, "Install or Update Userscript", "Could not open the browser: " + e.Message);
		}
	}
	
	[GeneratedRegex("^[a-zA-Z0-9]{1,100}$")]
	private static partial Regex ConnectionCodeTokenRegex();
	
	public async Task<bool> OnClickCopyConnectionCode() {
		const string Title = "Copy Connection Code";
		
		if (ConnectionCodeTokenRegex().IsMatch(ServerConfiguration.Token)) {
			return await TryCopy(ServerConfiguration.Port + ":" + ServerConfiguration.Token, Title);
		}
		else {
			await Dialog.ShowOk(window, Title, "The internal server token cannot be used to create a connection code.\n\nCheck the 'Advanced' tab and ensure the token is 1-100 characters long, and only contains plain letters and numbers.");
			return false;
		}
	}
	
	private async Task<bool> TryCopy(string script, string errorDialogTitle) {
		IClipboard? clipboard = window.Clipboard;
		if (clipboard == null) {
			await Dialog.ShowOk(window, errorDialogTitle, "Clipboard is not available on this system.");
			return false;
		}
		
		try {
			await clipboard.SetTextAsync(script);
			return true;
		} catch (Exception e) {
			Log.Error("Could not copy to clipboard.", e);
			await Dialog.ShowOk(window, errorDialogTitle, "An error occurred while copying to clipboard.");
			return false;
		}
	}
}
