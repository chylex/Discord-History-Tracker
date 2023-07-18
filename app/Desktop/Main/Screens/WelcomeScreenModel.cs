using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.Message;
using DHT.Server.Database;
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Screens;

sealed class WelcomeScreenModel : BaseModel, IDisposable {
	public string Version => Program.Version;

	public IDatabaseFile? Db { get; private set; }
	public bool HasDatabase => Db != null;

	private readonly Window window;

	private string? dbFilePath;

	[Obsolete("Designer")]
	public WelcomeScreenModel() : this(null!) {}

	public WelcomeScreenModel(Window window) {
		this.window = window;
	}

	public async void OpenOrCreateDatabase() {
		var path = await DatabaseGui.NewOpenOrCreateDatabaseFileDialog(window, Path.GetDirectoryName(dbFilePath));
		if (path != null) {
			await OpenOrCreateDatabaseFromPath(path);
		}
	}

	public async Task OpenOrCreateDatabaseFromPath(string path) {
		if (Db != null) {
			Db = null;
		}

		dbFilePath = path;
		Db = await DatabaseGui.TryOpenOrCreateDatabaseFromPath(path, window, CheckCanUpgradeDatabase);

		OnPropertyChanged(nameof(Db));
		OnPropertyChanged(nameof(HasDatabase));
	}

	private async Task<bool> CheckCanUpgradeDatabase() {
		return DialogResult.YesNo.Yes == await DatabaseGui.ShowCanUpgradeDatabaseDialog(window);
	}

	public void CloseDatabase() {
		Dispose();
		OnPropertyChanged(nameof(Db));
		OnPropertyChanged(nameof(HasDatabase));
	}

	public async void ShowAboutDialog() {
		await new AboutWindow { DataContext = new AboutWindowModel() }.ShowDialog(this.window);
	}

	public void Exit() {
		window.Close();
	}

	public void Dispose() {
		Db?.Dispose();
		Db = null;
	}
}
