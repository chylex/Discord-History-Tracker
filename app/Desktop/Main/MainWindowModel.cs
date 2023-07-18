using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Main.Screens;
using DHT.Desktop.Server;
using DHT.Server.Database;
using DHT.Utils.Models;

namespace DHT.Desktop.Main;

sealed class MainWindowModel : BaseModel, IDisposable {
	private const string DefaultTitle = "Discord History Tracker";

	public string Title { get; private set; } = DefaultTitle;

	public UserControl CurrentScreen { get; private set; }

	private readonly WelcomeScreen welcomeScreen;
	private readonly WelcomeScreenModel welcomeScreenModel;

	private MainContentScreen? mainContentScreen;
	private MainContentScreenModel? mainContentScreenModel;

	private readonly Window window;

	private IDatabaseFile? db;

	[Obsolete("Designer")]
	public MainWindowModel() : this(null!, Arguments.Empty) {}

	public MainWindowModel(Window window, Arguments args) {
		this.window = window;

		welcomeScreenModel = new WelcomeScreenModel(window);
		welcomeScreen = new WelcomeScreen { DataContext = welcomeScreenModel };
		CurrentScreen = welcomeScreen;

		welcomeScreenModel.PropertyChanged += WelcomeScreenModelOnPropertyChanged;

		var dbFile = args.DatabaseFile;
		if (!string.IsNullOrWhiteSpace(dbFile)) {
			async void OnWindowOpened(object? o, EventArgs eventArgs) {
				window.Opened -= OnWindowOpened;

				// https://github.com/AvaloniaUI/Avalonia/issues/3071
				if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					await Task.Delay(500);
				}

				if (File.Exists(dbFile)) {
					await welcomeScreenModel.OpenOrCreateDatabaseFromPath(dbFile);
				}
				else {
					await Dialog.ShowOk(window, "Database Error", "Database file not found:\n" + dbFile);
				}
			}

			window.Opened += OnWindowOpened;
		}

		if (args.ServerPort != null) {
			ServerManager.Port = args.ServerPort.Value;
		}

		if (args.ServerToken != null) {
			ServerManager.Token = args.ServerToken;
		}
	}

	private async void WelcomeScreenModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName == nameof(welcomeScreenModel.Db)) {
			if (mainContentScreenModel != null) {
				mainContentScreenModel.DatabaseClosed -= MainContentScreenModelOnDatabaseClosed;
				mainContentScreenModel.Dispose();
			}

			db?.Dispose();
			db = welcomeScreenModel.Db;

			if (db == null) {
				Title = DefaultTitle;
				mainContentScreenModel = null;
				mainContentScreen = null;
				CurrentScreen = welcomeScreen;
			}
			else {
				Title = Path.GetFileName(db.Path) + " - " + DefaultTitle;
				mainContentScreenModel = new MainContentScreenModel(window, db);
				await mainContentScreenModel.Initialize();
				mainContentScreenModel.DatabaseClosed += MainContentScreenModelOnDatabaseClosed;
				mainContentScreen = new MainContentScreen { DataContext = mainContentScreenModel };
				CurrentScreen = mainContentScreen;
			}

			OnPropertyChanged(nameof(CurrentScreen));
			OnPropertyChanged(nameof(Title));

			window.Focus();
		}
	}

	private void MainContentScreenModelOnDatabaseClosed(object? sender, EventArgs e) {
		welcomeScreenModel.CloseDatabase();
	}

	public void Dispose() {
		welcomeScreenModel.Dispose();
		mainContentScreenModel?.Dispose();
		db?.Dispose();
		db = null;
	}
}
