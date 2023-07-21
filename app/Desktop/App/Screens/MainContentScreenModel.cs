using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.App.Controls;
using DHT.Desktop.App.Dialogs.Message;
using DHT.Desktop.App.Pages;
using DHT.Desktop.Server;
using DHT.Server.Database;
using DHT.Utils.Logging;

namespace DHT.Desktop.App.Screens;

sealed class MainContentScreenModel : IDisposable {
	private static readonly Log Log = Log.ForType<MainContentScreenModel>();

	public DatabasePage DatabasePage { get; }
	private DatabasePageModel DatabasePageModel { get; }

	public TrackingPage TrackingPage { get; }
	private TrackingPageModel TrackingPageModel { get; }

	public AdvancedPage AdvancedPage { get; }
	private AdvancedPageModel AdvancedPageModel { get; }

	public StatusBarModel StatusBarModel { get; }

	public event EventHandler? DatabaseClosed {
		add {
			DatabasePageModel.DatabaseClosed += value;
		}
		remove {
			DatabasePageModel.DatabaseClosed -= value;
		}
	}

	private readonly Window window;
	private readonly ServerManager serverManager;

	[Obsolete("Designer")]
	public MainContentScreenModel() : this(null!, DummyDatabaseFile.Instance) {}

	public MainContentScreenModel(Window window, IDatabaseFile db) {
		this.window = window;
		this.serverManager = new ServerManager(db);

		ServerLauncher.ServerManagementExceptionCaught += ServerLauncherOnServerManagementExceptionCaught;

		DatabasePageModel = new DatabasePageModel(window, db);
		DatabasePage = new DatabasePage { DataContext = DatabasePageModel };

		TrackingPageModel = new TrackingPageModel(window);
		TrackingPage = new TrackingPage { DataContext = TrackingPageModel };

		AdvancedPageModel = new AdvancedPageModel(window, db, serverManager);
		AdvancedPage = new AdvancedPage { DataContext = AdvancedPageModel };

		StatusBarModel = new StatusBarModel();

		AdvancedPageModel.ServerConfigurationModel.ServerStatusChanged += OnServerStatusChanged;
		DatabaseClosed += OnDatabaseClosed;

		StatusBarModel.CurrentStatus = serverManager.IsRunning ? StatusBarModel.Status.Ready : StatusBarModel.Status.Stopped;
	}

	public async Task Initialize() {
		await TrackingPageModel.Initialize();
		AdvancedPageModel.Initialize();
		serverManager.Launch();
	}

	public void Dispose() {
		ServerLauncher.ServerManagementExceptionCaught -= ServerLauncherOnServerManagementExceptionCaught;
		serverManager.Dispose();
	}

	private void OnServerStatusChanged(object? sender, StatusBarModel.Status e) {
		StatusBarModel.CurrentStatus = e;
	}

	private void OnDatabaseClosed(object? sender, EventArgs e) {
		serverManager.Stop();
	}

	private async void ServerLauncherOnServerManagementExceptionCaught(object? sender, Exception ex) {
		Log.Error(ex);
		await Dialog.ShowOk(window, "Internal Server Error", ex.Message);
	}
}
