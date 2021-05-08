using System;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DHT.Desktop.Dialogs;
using DHT.Desktop.Main.Controls;
using DHT.Desktop.Models;
using DHT.Desktop.Resources;
using DHT.Server.Database;
using DHT.Server.Logging;
using DHT.Server.Service;

namespace DHT.Desktop.Main.Pages {
	public class TrackingPageModel : BaseModel, IDisposable {
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

		private bool isToggleButtonEnabled = true;

		public bool IsToggleButtonEnabled {
			get => isToggleButtonEnabled;
			set => Change(ref isToggleButtonEnabled, value);
		}

		public string ToggleButtonText => ServerLauncher.IsRunning ? "Pause Tracking" : "Resume Tracking";

		public event EventHandler<StatusBarModel.Status>? ServerStatusChanged;

		private readonly Window window;
		private readonly IDatabaseFile db;

		[Obsolete("Designer")]
		public TrackingPageModel() : this(null!, DummyDatabaseFile.Instance) {}

		public TrackingPageModel(Window window, IDatabaseFile db) {
			this.window = window;
			this.db = db;
		}

		public void Initialize() {
			ServerLauncher.ServerStatusChanged += ServerLauncherOnServerStatusChanged;
			ServerLauncher.ServerManagementExceptionCaught += ServerLauncherOnServerManagementExceptionCaught;

			if (int.TryParse(ServerPort, out int port)) {
				string token = ServerToken;
				ServerLauncher.Relaunch(port, token, db);
			}
		}

		public void Dispose() {
			ServerLauncher.ServerManagementExceptionCaught -= ServerLauncherOnServerManagementExceptionCaught;
			ServerLauncher.ServerStatusChanged -= ServerLauncherOnServerStatusChanged;
			ServerLauncher.Stop();
			GC.SuppressFinalize(this);
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

		public async Task<bool> OnClickToggleButton() {
			if (ServerLauncher.IsRunning) {
				StopServer();
				return true;
			}
			else {
				return await StartServer();
			}
		}

		public async Task OnClickCopyTrackingScript() {
			string bootstrap = await ResourceLoader.ReadTextAsync("Tracker/bootstrap.js");
			string script = bootstrap.Replace("= 0; /*[PORT]*/", "= " + ServerPort + ";")
			                         .Replace("/*[TOKEN]*/", HttpUtility.JavaScriptStringEncode(ServerToken))
			                         .Replace("/*[IMPORTS]*/", await ResourceLoader.ReadJoinedAsync("Tracker/scripts/", '\n'))
			                         .Replace("/*[CSS-CONTROLLER]*/", await ResourceLoader.ReadTextAsync("Tracker/styles/controller.css"))
			                         .Replace("/*[CSS-SETTINGS]*/", await ResourceLoader.ReadTextAsync("Tracker/styles/settings.css"));

			await Application.Current.Clipboard.SetTextAsync(script);
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

		private void ServerLauncherOnServerStatusChanged(object? sender, EventArgs e) {
			ServerStatusChanged?.Invoke(this, ServerLauncher.IsRunning ? StatusBarModel.Status.Ready : StatusBarModel.Status.Stopped);
			OnPropertyChanged(nameof(ToggleButtonText));
			IsToggleButtonEnabled = true;
		}

		private void ServerLauncherOnServerManagementExceptionCaught(object? sender, Exception ex) {
			Log.Error(ex);

			string message = ex.Message;
			Dispatcher.UIThread.Post(async () => { await Dialog.ShowOk(window, "Server Error", message); });
		}
	}
}
