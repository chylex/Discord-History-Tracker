using System;
using Avalonia.Controls;
using DHT.Desktop.Main.Controls;
using DHT.Server;

namespace DHT.Desktop.Main.Pages;

sealed class AdvancedPageModel : IDisposable {
	public ServerConfigurationPanelModel ServerConfigurationModel { get; }
	
	[Obsolete("Designer")]
	public AdvancedPageModel() : this(null!, State.Dummy) {}
	
	public AdvancedPageModel(Window window, State state) {
		ServerConfigurationModel = new ServerConfigurationPanelModel(window, state);
	}
	
	public void Dispose() {
		ServerConfigurationModel.Dispose();
	}
}
