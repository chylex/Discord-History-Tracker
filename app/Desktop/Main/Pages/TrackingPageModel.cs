using System;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Discord;
using DHT.Desktop.Main.Controls;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Logging;
using DHT.Utils.Models;
using static DHT.Desktop.Program;

namespace DHT.Desktop.Main.Pages {
	sealed class TrackingPageModel : BaseModel, IDisposable {
		private static readonly Log Log = Log.ForType<TrackingPageModel>();

		internal static string ServerPort { get; set; } = ServerUtils.FindAvailablePort(50000, 60000).ToString();
		internal static string ServerToken { get; set; } = ServerUtils.GenerateRandomToken(20);

		private string inputPort = ServerPort;

		public string InputPort {
			get => inputPort;
			set {
				Change(ref inputPort, value);
				OnPropertyChanged(nameof(HasMadeChanges));
			}
		}

		private string inputToken = ServerToken;

		public string InputToken {
			get => inputToken;
			set {
				Change(ref inputToken, value);
				OnPropertyChanged(nameof(HasMadeChanges));
			}
		}

		public bool HasMadeChanges => ServerPort != InputPort || ServerToken != InputToken;

		private bool isToggleTrackingButtonEnabled = true;

		public bool IsToggleButtonEnabled {
			get => isToggleTrackingButtonEnabled;
			set => Change(ref isToggleTrackingButtonEnabled, value);
		}

		public string ToggleTrackingButtonText => ServerLauncher.IsRunning ? "Pause Tracking" : "Resume Tracking";

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

		public event EventHandler<StatusBarModel.Status>? ServerStatusChanged;

		private readonly Window window;
		private readonly IDatabaseFile db;

		[Obsolete("Designer")]
		public TrackingPageModel() : this(null!, DummyDatabaseFile.Instance) {}

		public TrackingPageModel(Window window, IDatabaseFile db) {
			this.window = window;
			this.db = db;
		}

		public async Task Initialize() {
			ServerLauncher.ServerStatusChanged += ServerLauncherOnServerStatusChanged;
			ServerLauncher.ServerManagementExceptionCaught += ServerLauncherOnServerManagementExceptionCaught;

			if (int.TryParse(ServerPort, out int port)) {
				string token = ServerToken;
				ServerLauncher.Relaunch(port, token, db);
			}

			bool? devToolsEnabled = await DiscordAppSettings.AreDevToolsEnabled();
			if (devToolsEnabled.HasValue) {
				AreDevToolsEnabled = devToolsEnabled.Value;
			}
			else {
				IsToggleAppDevToolsButtonEnabled = false;
				OnPropertyChanged(nameof(IsToggleAppDevToolsButtonEnabled));
			}
		}

		public void Dispose() {
			ServerLauncher.ServerManagementExceptionCaught -= ServerLauncherOnServerManagementExceptionCaught;
			ServerLauncher.ServerStatusChanged -= ServerLauncherOnServerStatusChanged;
			ServerLauncher.Stop();
		}

		private void ServerLauncherOnServerStatusChanged(object? sender, EventArgs e) {
			ServerStatusChanged?.Invoke(this, ServerLauncher.IsRunning ? StatusBarModel.Status.Ready : StatusBarModel.Status.Stopped);
			OnPropertyChanged(nameof(ToggleTrackingButtonText));
			IsToggleButtonEnabled = true;
		}

		private async void ServerLauncherOnServerManagementExceptionCaught(object? sender, Exception ex) {
			Log.Error(ex);
			await Dialog.ShowOk(window, "Server Error", ex.Message);
		}

		private async Task<bool> StartServer() {
			if (!int.TryParse(InputPort, out int port) || port is < 0 or > 65535) {
				await Dialog.ShowOk(window, "Invalid Port", "Port must be a number between 0 and 65535.");
				return false;
			}

			IsToggleButtonEnabled = false;
			ServerStatusChanged?.Invoke(this, StatusBarModel.Status.Starting);
			ServerLauncher.Relaunch(port, InputToken, db);
			return true;
		}

		private void StopServer() {
			IsToggleButtonEnabled = false;
			ServerStatusChanged?.Invoke(this, StatusBarModel.Status.Stopping);
			ServerLauncher.Stop();
		}

		public async Task<bool> OnClickToggleTrackingButton() {
			if (ServerLauncher.IsRunning) {
				StopServer();
				return true;
			}
			else {
				return await StartServer();
			}
		}

		public async Task<bool> OnClickCopyTrackingScript() {
			string bootstrap = await Resources.ReadTextAsync("Tracker/bootstrap.js");
			string script = bootstrap.Replace("= 0; /*[PORT]*/", "= " + ServerPort + ";")
			                         .Replace("/*[TOKEN]*/", HttpUtility.JavaScriptStringEncode(ServerToken))
			                         .Replace("/*[IMPORTS]*/", await Resources.ReadJoinedAsync("Tracker/scripts/", '\n'))
			                         .Replace("/*[CSS-CONTROLLER]*/", await Resources.ReadTextAsync("Tracker/styles/controller.css"))
			                         .Replace("/*[CSS-SETTINGS]*/", await Resources.ReadTextAsync("Tracker/styles/settings.css"));

			var clipboard = Application.Current?.Clipboard;
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

		public void OnClickRandomizeToken() {
			InputToken = ServerUtils.GenerateRandomToken(20);
		}

		public async void OnClickApplyChanges() {
			if (await StartServer()) {
				ServerPort = InputPort;
				ServerToken = InputToken;
				OnPropertyChanged(nameof(HasMadeChanges));
			}
		}

		public void OnClickCancelChanges() {
			InputPort = ServerPort;
			InputToken = ServerToken;
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
}
