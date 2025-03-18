using System;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using DHT.Server;
using DHT.Server.Service;
using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Main.Controls;

sealed partial class StatusBarModel : IDisposable {
	[Notify(Setter.Private)]
	private long? serverCount;
	
	[Notify(Setter.Private)]
	private long? channelCount;
	
	[Notify(Setter.Private)]
	private long? messageCount;
	
	[Notify(Setter.Private)]
	private ServerManager.Status serverStatus;
	
	[DependsOn(nameof(ServerStatus))]
	public string ServerStatusText => ServerStatus switch {
		ServerManager.Status.Starting => "STARTING",
		ServerManager.Status.Started  => "READY",
		ServerManager.Status.Stopping => "STOPPING",
		ServerManager.Status.Stopped  => "STOPPED",
		_                             => "",
	};
	
	private readonly State state;
	private readonly IDisposable serverCountSubscription;
	private readonly IDisposable channelCountSubscription;
	private readonly IDisposable messageCountSubscription;
	
	[Obsolete("Designer")]
	public StatusBarModel() : this(State.Dummy) {}
	
	public StatusBarModel(State state) {
		this.state = state;
		
		serverCountSubscription = state.Db.Servers.TotalCount.ObserveOn(AvaloniaScheduler.Instance).Subscribe(newServerCount => ServerCount = newServerCount);
		channelCountSubscription = state.Db.Channels.TotalCount.ObserveOn(AvaloniaScheduler.Instance).Subscribe(newChannelCount => ChannelCount = newChannelCount);
		messageCountSubscription = state.Db.Messages.TotalCount.ObserveOn(AvaloniaScheduler.Instance).Subscribe(newMessageCount => MessageCount = newMessageCount);
		
		state.Server.StatusChanged += OnStateServerStatusChanged;
		serverStatus = state.Server.IsRunning ? ServerManager.Status.Started : ServerManager.Status.Stopped;
	}
	
	public void Dispose() {
		serverCountSubscription.Dispose();
		channelCountSubscription.Dispose();
		messageCountSubscription.Dispose();
		
		state.Server.StatusChanged -= OnStateServerStatusChanged;
	}
	
	private void OnStateServerStatusChanged(object? sender, ServerManager.Status e) {
		Dispatcher.UIThread.InvokeAsync(() => ServerStatus = e);
	}
}
