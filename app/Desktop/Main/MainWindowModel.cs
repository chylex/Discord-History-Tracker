using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Main.Screens;
using DHT.Desktop.Server;
using DHT.Server;
using DHT.Utils.Logging;
using DHT.Utils.Models;

namespace DHT.Desktop.Main;

sealed class MainWindowModel : BaseModel, IAsyncDisposable {
	private const string DefaultTitle = "Discord History Tracker";
	
	private static readonly Log Log = Log.ForType<MainWindowModel>();

	public string Title { get; private set; } = DefaultTitle;

	public UserControl CurrentScreen { get; private set; }

	private readonly WelcomeScreen welcomeScreen;
	private readonly WelcomeScreenModel welcomeScreenModel;

	private MainContentScreen? mainContentScreen;
	private MainContentScreenModel? mainContentScreenModel;

	private readonly Window window;

	private State? state;

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
			ServerConfiguration.Port = args.ServerPort.Value;
		}

		if (args.ServerToken != null) {
			ServerConfiguration.Token = args.ServerToken;
		}
	}

	private async void WelcomeScreenModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName == nameof(welcomeScreenModel.Db)) {
			if (mainContentScreenModel != null) {
				mainContentScreenModel.DatabaseClosed -= MainContentScreenModelOnDatabaseClosed;
				mainContentScreenModel.Dispose();
			}

			if (state != null) {
				await state.DisposeAsync();
			}
			
			if (welcomeScreenModel.Db == null) {
				state = null;
				Title = DefaultTitle;
				mainContentScreenModel = null;
				mainContentScreen = null;
				CurrentScreen = welcomeScreen;
			}
			else {
				state = new State(welcomeScreenModel.Db);

				try {
					await state.Server.Start(ServerConfiguration.Port, ServerConfiguration.Token);
				} catch (Exception ex) {
					Log.Error(ex);
					await Dialog.ShowOk(window, "Internal Server Error", ex.Message);
				}
				
				Title = Path.GetFileName(state.Db.Path) + " - " + DefaultTitle;
				mainContentScreenModel = new MainContentScreenModel(window, state);
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

	public async ValueTask DisposeAsync() {
		mainContentScreenModel?.Dispose();

		if (state != null) {
			await state.DisposeAsync();
			state = null;
		}
		
		welcomeScreenModel.Dispose();
	}
}
