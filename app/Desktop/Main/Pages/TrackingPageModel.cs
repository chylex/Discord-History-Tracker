using System;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Discord;
using DHT.Desktop.Server;
using DHT.Utils.Models;
using static DHT.Desktop.Program;

namespace DHT.Desktop.Main.Pages;

sealed class TrackingPageModel : BaseModel {
	private bool areDevToolsEnabled;

	private bool AreDevToolsEnabled {
		get => areDevToolsEnabled;
		set {
			Change(ref areDevToolsEnabled, value);
			OnPropertyChanged(nameof(ToggleAppDevToolsButtonText));
		}
	}

	public bool IsToggleAppDevToolsButtonEnabled { get; private set; } = true;

	public string ToggleAppDevToolsButtonText {
		get {
			if (!IsToggleAppDevToolsButtonEnabled) {
				return "Unavailable";
			}

			return AreDevToolsEnabled ? "Disable Ctrl+Shift+I" : "Enable Ctrl+Shift+I";
		}
	}

	private readonly Window window;

	[Obsolete("Designer")]
	public TrackingPageModel() : this(null!) {}

	public TrackingPageModel(Window window) {
		this.window = window;
	}

	public async Task Initialize() {
		bool? devToolsEnabled = await DiscordAppSettings.AreDevToolsEnabled();
		if (devToolsEnabled.HasValue) {
			AreDevToolsEnabled = devToolsEnabled.Value;
		}
		else {
			IsToggleAppDevToolsButtonEnabled = false;
			OnPropertyChanged(nameof(IsToggleAppDevToolsButtonEnabled));
		}
	}

	public async Task<bool> OnClickCopyTrackingScript() {
		string bootstrap = await Resources.ReadTextAsync("Tracker/bootstrap.js");
		string script = bootstrap.Replace("= 0; /*[PORT]*/", "= " + ServerManager.Port + ";")
		                         .Replace("/*[TOKEN]*/", HttpUtility.JavaScriptStringEncode(ServerManager.Token))
		                         .Replace("/*[IMPORTS]*/", await Resources.ReadJoinedAsync("Tracker/scripts/", '\n'))
		                         .Replace("/*[CSS-CONTROLLER]*/", await Resources.ReadTextAsync("Tracker/styles/controller.css"))
		                         .Replace("/*[CSS-SETTINGS]*/", await Resources.ReadTextAsync("Tracker/styles/settings.css"));

		var clipboard = window.Clipboard;
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

	public async void OnClickToggleAppDevTools() {
		const string DialogTitle = "Discord App Settings File";

		bool oldState = AreDevToolsEnabled;
		bool newState = !oldState;

		switch (await DiscordAppSettings.ConfigureDevTools(newState)) {
			case SettingsJsonResult.Success:
				AreDevToolsEnabled = newState;
				await Dialog.ShowOk(window, DialogTitle, "Ctrl+Shift+I was " + (newState ? "enabled." : "disabled.") + " Restart the Discord app for the change to take effect.");
				break;

			case SettingsJsonResult.AlreadySet:
				await Dialog.ShowOk(window, DialogTitle, "Ctrl+Shift+I is already " + (newState ? "enabled." : "disabled."));
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
