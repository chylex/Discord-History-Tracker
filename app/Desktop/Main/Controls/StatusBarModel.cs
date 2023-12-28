using System;
using Avalonia.Threading;
using DHT.Server;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Controls;

sealed class StatusBarModel : BaseModel, IDisposable {
	public DatabaseStatistics DatabaseStatistics { get; }

	private ServerManager.Status serverStatus;

	public string ServerStatusText => serverStatus switch {
		ServerManager.Status.Starting => "STARTING",
		ServerManager.Status.Started  => "READY",
		ServerManager.Status.Stopping => "STOPPING",
		ServerManager.Status.Stopped  => "STOPPED",
		_                             => ""
	};

	private readonly State state;

	[Obsolete("Designer")]
	public StatusBarModel() : this(State.Dummy) {}

	public StatusBarModel(State state) {
		this.state = state;
		this.DatabaseStatistics = state.Db.Statistics;

		state.Server.StatusChanged += OnServerStatusChanged;
		serverStatus = state.Server.IsRunning ? ServerManager.Status.Started : ServerManager.Status.Stopped;
	}

	public void Dispose() {
		state.Server.StatusChanged -= OnServerStatusChanged;
	}

	private void OnServerStatusChanged(object? sender, ServerManager.Status e) {
		Dispatcher.UIThread.InvokeAsync(() => {
			serverStatus = e;
			OnPropertyChanged(nameof(ServerStatusText));
		});
	}
}
