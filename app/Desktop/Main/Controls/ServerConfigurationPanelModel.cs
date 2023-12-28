using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Server;
using DHT.Server;
using DHT.Server.Service;
using DHT.Utils.Logging;
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Controls;

sealed class ServerConfigurationPanelModel : BaseModel, IDisposable {
	private static readonly Log Log = Log.ForType<ServerConfigurationPanelModel>();
	
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

	public bool HasMadeChanges => ServerConfiguration.Port.ToString() != InputPort || ServerConfiguration.Token != InputToken;

	private bool isToggleServerButtonEnabled = true;

	public bool IsToggleServerButtonEnabled {
		get => isToggleServerButtonEnabled;
		set => Change(ref isToggleServerButtonEnabled, value);
	}

	public string ToggleServerButtonText => server.IsRunning ? "Stop Server" : "Start Server";

	private readonly Window window;
	private readonly ServerManager server;

	[Obsolete("Designer")]
	public ServerConfigurationPanelModel() : this(null!, State.Dummy) {}

	public ServerConfigurationPanelModel(Window window, State state) {
		this.window = window;
		this.server = state.Server;
		this.inputPort = ServerConfiguration.Port.ToString();
		this.inputToken = ServerConfiguration.Token;
		
		server.StatusChanged += OnServerStatusChanged;
	}

	public void Dispose() {
		server.StatusChanged -= OnServerStatusChanged;
	}

	private void OnServerStatusChanged(object? sender, ServerManager.Status e) {
		Dispatcher.UIThread.InvokeAsync(UpdateServerStatus);
	}

	private void UpdateServerStatus() {
		OnPropertyChanged(nameof(ToggleServerButtonText));
	}

	private async Task StartServer() {
		IsToggleServerButtonEnabled = false;
		
		try {
			await server.Start(ServerConfiguration.Port, ServerConfiguration.Token);
		} catch (Exception e) {
			Log.Error(e);
			await Dialog.ShowOk(window, "Internal Server Error", e.Message);
		}
		
		UpdateServerStatus();
		IsToggleServerButtonEnabled = true;
	}

	private async Task StopServer() {
		IsToggleServerButtonEnabled = false;
		
		try {
			await server.Stop();
		} catch (Exception e) {
			Log.Error(e);
			await Dialog.ShowOk(window, "Internal Server Error", e.Message);
		}
		
		UpdateServerStatus();
		IsToggleServerButtonEnabled = true;
	}

	public async Task OnClickToggleServerButton() {
		if (server.IsRunning) {
			await StopServer();
		}
		else {
			await StartServer();
		}
	}

	public void OnClickRandomizeToken() {
		InputToken = ServerUtils.GenerateRandomToken(20);
	}

	public async Task OnClickApplyChanges() {
		if (!ushort.TryParse(InputPort, out ushort port)) {
			await Dialog.ShowOk(window, "Invalid Port", "Port must be a number between 0 and 65535.");
			return;
		}

		ServerConfiguration.Port = port;
		ServerConfiguration.Token = inputToken;
		
		OnPropertyChanged(nameof(HasMadeChanges));
		
		await StartServer();
	}

	public void OnClickCancelChanges() {
		InputPort = ServerConfiguration.Port.ToString();
		InputToken = ServerConfiguration.Token;
	}
}
