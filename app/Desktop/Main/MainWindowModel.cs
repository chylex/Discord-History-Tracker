using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Desktop.Main.Screens;
using DHT.Desktop.Server;
using DHT.Server;
using DHT.Server.Database;
using DHT.Utils.Logging;
using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Main;

sealed partial class MainWindowModel : IAsyncDisposable {
	private const string DefaultTitle = "Discord History Tracker";
	
	private static readonly Log Log = Log.ForType<MainWindowModel>();
	
	[Notify(Setter.Private)]
	private string title = DefaultTitle;
	
	[Notify(Setter.Private)]
	private UserControl currentScreen;
	
	private readonly WelcomeScreen welcomeScreen;
	private readonly WelcomeScreenModel welcomeScreenModel;
	
	private MainContentScreenModel? mainContentScreenModel;
	
	private readonly Window window;
	private readonly int? concurrentDownloads;
	
	private State? state;
	
	[Obsolete("Designer")]
	public MainWindowModel() : this(null!, Arguments.Empty) {}
	
	public MainWindowModel(Window window, Arguments args) {
		this.window = window;
		
		welcomeScreenModel = new WelcomeScreenModel(window);
		welcomeScreenModel.DatabaseSelected += OnDatabaseSelected;
		
		welcomeScreen = new WelcomeScreen { DataContext = welcomeScreenModel };
		currentScreen = welcomeScreen;
		
		string? dbFile = args.DatabaseFile;
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
		
		concurrentDownloads = args.ConcurrentDownloads;
	}
	
	private async void OnDatabaseSelected(object? sender, IDatabaseFile db) {
		welcomeScreenModel.DatabaseSelected -= OnDatabaseSelected;
		
		await DisposeState();
		
		state = new State(db, concurrentDownloads);
		
		try {
			await state.Server.Start(ServerConfiguration.Port, ServerConfiguration.Token);
		} catch (Exception ex) {
			Log.Error("Could not start internal server.", ex);
			await Dialog.ShowOk(window, "Internal Server Error", ex.Message);
		}
		
		mainContentScreenModel = new MainContentScreenModel(window, state);
		mainContentScreenModel.DatabaseClosed += MainContentScreenModelOnDatabaseClosed;
		
		await mainContentScreenModel.Initialize();
		
		Title = Path.GetFileName(state.Db.Path) + " - " + DefaultTitle;
		CurrentScreen = new MainContentScreen { DataContext = mainContentScreenModel };
		
		window.Focus();
	}
	
	private async void MainContentScreenModelOnDatabaseClosed(object? sender, EventArgs e) {
		await DisposeContent();
		
		Title = DefaultTitle;
		CurrentScreen = welcomeScreen;
		
		await DisposeState();
		
		welcomeScreenModel.DatabaseSelected += OnDatabaseSelected;
	}
	
	private async Task DisposeContent() {
		if (mainContentScreenModel != null) {
			mainContentScreenModel.DatabaseClosed -= MainContentScreenModelOnDatabaseClosed;
			await mainContentScreenModel.DisposeAsync();
			mainContentScreenModel = null;
		}
	}
	
	private async Task DisposeState() {
		if (state != null) {
			await DelayedProgressDialog.Await(() => state.DisposeAsync().AsTask(), TimeSpan.FromMilliseconds(200), window, "Close Database", "Please wait for the database to close...");
			state = null;
		}
	}
	
	public async ValueTask DisposeAsync() {
		await DisposeContent();
		await DisposeState();
	}
}
