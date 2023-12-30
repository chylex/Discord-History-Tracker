using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Desktop.Main.Controls;
using DHT.Server;

namespace DHT.Desktop.Main.Pages;

sealed class AdvancedPageModel : IDisposable {
	public ServerConfigurationPanelModel ServerConfigurationModel { get; }
	
	private readonly Window window;
	private readonly State state;

	[Obsolete("Designer")]
	public AdvancedPageModel() : this(null!, State.Dummy) {}

	public AdvancedPageModel(Window window, State state) {
		this.window = window;
		this.state = state;

		ServerConfigurationModel = new ServerConfigurationPanelModel(window, state);
	}

	public void Dispose() {
		ServerConfigurationModel.Dispose();
	}

	public async Task VacuumDatabase() {
		const string Title = "Vacuum Database";
		await ProgressDialog.ShowIndeterminate(window, Title, "Vacuuming database...", _ => state.Db.Vacuum());
		await Dialog.ShowOk(window, Title, "Done.");
	}
}
