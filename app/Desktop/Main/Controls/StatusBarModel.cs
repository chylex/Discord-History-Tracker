using System;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DHT.Server;
using DHT.Server.Service;

namespace DHT.Desktop.Main.Controls;

sealed partial class StatusBarModel : ObservableObject, IDisposable {
	[ObservableProperty]
	public partial long? ServerCount { get; private set; }
	
	[ObservableProperty]
	public partial long? ChannelCount { get; private set; }
	
	[ObservableProperty]
	public partial long? MessageCount { get; private set; }
	
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ServerStatusText))]
	public partial ServerManager.Status ServerStatus { get; private set; }
	
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
		
		state.Server.StatusChanged += OnServerStatusChanged;
		ServerStatus = state.Server.IsRunning ? ServerManager.Status.Started : ServerManager.Status.Stopped;
	}
	
	public void Dispose() {
		serverCountSubscription.Dispose();
		channelCountSubscription.Dispose();
		messageCountSubscription.Dispose();
		
		state.Server.StatusChanged -= OnServerStatusChanged;
	}
	
	private void OnServerStatusChanged(object? sender, ServerManager.Status e) {
		Dispatcher.UIThread.InvokeAsync(() => ServerStatus = e);
	}
}
