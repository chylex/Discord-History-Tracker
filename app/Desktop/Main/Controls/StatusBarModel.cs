using System;
using Avalonia.Threading;
using DHT.Desktop.Common;
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
		
		serverCountSubscription = state.Db.Servers.TotalCount.SubscribeLastOnUI(newServerCount => ServerCount = newServerCount, TimeSpan.FromMilliseconds(15));
		channelCountSubscription = state.Db.Channels.TotalCount.SubscribeLastOnUI(newChannelCount => ChannelCount = newChannelCount, TimeSpan.FromMilliseconds(15));
		messageCountSubscription = state.Db.Messages.TotalCount.SubscribeLastOnUI(newMessageCount => MessageCount = newMessageCount, TimeSpan.FromMilliseconds(15));
		
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
