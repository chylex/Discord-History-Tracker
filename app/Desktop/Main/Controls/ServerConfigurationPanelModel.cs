using System;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Server;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Controls;

sealed class ServerConfigurationPanelModel : BaseModel, IDisposable {
	private string inputPort;

	public string InputPort {
		get => inputPort;
		set {
			Change(ref inputPort, value);
			OnPropertyChanged(nameof(HasMadeChanges));
		}
	}

	private string inputToken;

	public string InputToken {
		get => inputToken;
		set {
			Change(ref inputToken, value);
			OnPropertyChanged(nameof(HasMadeChanges));
		}
	}

	public bool HasMadeChanges => ServerManager.Port.ToString() != InputPort || ServerManager.Token != InputToken;

	private bool isToggleServerButtonEnabled = true;

	public bool IsToggleServerButtonEnabled {
		get => isToggleServerButtonEnabled;
		set => Change(ref isToggleServerButtonEnabled, value);
	}

	public string ToggleServerButtonText => serverManager.IsRunning ? "Stop Server" : "Start Server";

	public event EventHandler<StatusBarModel.Status>? ServerStatusChanged;

	private readonly Window window;
	private readonly ServerManager serverManager;

	[Obsolete("Designer")]
	public ServerConfigurationPanelModel() : this(null!, new ServerManager(DummyDatabaseFile.Instance)) {}

	public ServerConfigurationPanelModel(Window window, ServerManager serverManager) {
		this.window = window;
		this.serverManager = serverManager;
		this.inputPort = ServerManager.Port.ToString();
		this.inputToken = ServerManager.Token;
	}

	public void Initialize() {
		ServerLauncher.ServerStatusChanged += ServerLauncherOnServerStatusChanged;
	}

	public void Dispose() {
		ServerLauncher.ServerStatusChanged -= ServerLauncherOnServerStatusChanged;
	}

	private void ServerLauncherOnServerStatusChanged(object? sender, EventArgs e) {
		ServerStatusChanged?.Invoke(this, serverManager.IsRunning ? StatusBarModel.Status.Ready : StatusBarModel.Status.Stopped);
		OnPropertyChanged(nameof(ToggleServerButtonText));
		IsToggleServerButtonEnabled = true;
	}

	private void BeforeServerStart() {
		IsToggleServerButtonEnabled = false;
		ServerStatusChanged?.Invoke(this, StatusBarModel.Status.Starting);
	}

	private void StartServer() {
		BeforeServerStart();
		serverManager.Launch();
	}

	private void StopServer() {
		IsToggleServerButtonEnabled = false;
		ServerStatusChanged?.Invoke(this, StatusBarModel.Status.Stopping);
		serverManager.Stop();
	}

	public void OnClickToggleServerButton() {
		if (serverManager.IsRunning) {
			StopServer();
		}
		else {
			StartServer();
		}
	}

	public void OnClickRandomizeToken() {
		InputToken = ServerUtils.GenerateRandomToken(20);
	}

	public async void OnClickApplyChanges() {
		if (!ushort.TryParse(InputPort, out ushort port)) {
			await Dialog.ShowOk(window, "Invalid Port", "Port must be a number between 0 and 65535.");
			return;
		}

		BeforeServerStart();
		serverManager.Relaunch(port, InputToken);
		OnPropertyChanged(nameof(HasMadeChanges));
	}

	public void OnClickCancelChanges() {
		InputPort = ServerManager.Port.ToString();
		InputToken = ServerManager.Token;
	}
}
