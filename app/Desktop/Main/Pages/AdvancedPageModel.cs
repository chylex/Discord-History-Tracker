using System;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Main.Controls;
using DHT.Desktop.Server;
using DHT.Server;
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Pages;

sealed class AdvancedPageModel : BaseModel, IDisposable {
	public ServerConfigurationPanelModel ServerConfigurationModel { get; }

	private readonly Window window;
	private readonly State state;

	[Obsolete("Designer")]
	public AdvancedPageModel() : this(null!, State.Dummy, new ServerManager(State.Dummy)) {}

	public AdvancedPageModel(Window window, State state, ServerManager serverManager) {
		this.window = window;
		this.state = state;

		ServerConfigurationModel = new ServerConfigurationPanelModel(window, serverManager);
	}

	public void Initialize() {
		ServerConfigurationModel.Initialize();
	}

	public void Dispose() {
		ServerConfigurationModel.Dispose();
	}

	public async void VacuumDatabase() {
		state.Db.Vacuum();
		await Dialog.ShowOk(window, "Vacuum Database", "Done.");
	}
}
