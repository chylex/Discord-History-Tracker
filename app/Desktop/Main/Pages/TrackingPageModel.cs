using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Discord;
using DHT.Desktop.Server;
using PropertyChanged.SourceGenerator;
using static DHT.Desktop.Program;

namespace DHT.Desktop.Main.Pages;

sealed partial class TrackingPageModel {
	[Notify(Setter.Private)]
	private bool isCopyTrackingScriptButtonEnabled = true;
	
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
		IsCopyTrackingScriptButtonEnabled = false;
		
		try {
			return await CopyTrackingScript();
		} finally {
			IsCopyTrackingScriptButtonEnabled = true;
		}
	}
	
	private async Task<bool> CopyTrackingScript() {
		string url = $"http://127.0.0.1:{ServerConfiguration.Port}/get-tracking-script?token={HttpUtility.UrlEncode(ServerConfiguration.Token)}";
		string script = (await Resources.ReadTextAsync("tracker-loader.js")).Trim().Replace("{url}", url);
		
		IClipboard? clipboard = window.Clipboard;
		if (clipboard == null) {
			await Dialog.ShowOk(window, "Copy Tracking Script", "Clipboard is not available on this system.");
			return false;
		}
		
		try {
			await clipboard.SetTextAsync(script);
			return true;
		} catch {
			await Dialog.ShowOk(window, "Copy Tracking Script", "An error occurred while copying to clipboard.");
			return false;
		}
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
}
