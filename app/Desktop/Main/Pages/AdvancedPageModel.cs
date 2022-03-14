using System;
using Avalonia.Controls;
using DHT.Desktop.Main.Controls;
using DHT.Desktop.Server;
using DHT.Server.Database;
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Pages {
	sealed class AdvancedPageModel : BaseModel, IDisposable {
		public ServerConfigurationPanelModel ServerConfigurationModel { get; }

		[Obsolete("Designer")]
		public AdvancedPageModel() : this(null!, new ServerManager(DummyDatabaseFile.Instance)) {}

		public AdvancedPageModel(Window window, ServerManager serverManager) {
			ServerConfigurationModel = new ServerConfigurationPanelModel(window, serverManager);
		}

		public void Initialize() {
			ServerConfigurationModel.Initialize();
		}
		
		public void Dispose() {
			ServerConfigurationModel.Dispose();
		}
	}
}
