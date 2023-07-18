using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Main.Controls;
using DHT.Desktop.Main.Pages;
using DHT.Desktop.Server;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Logging;

namespace DHT.Desktop.Main.Screens;

sealed class MainContentScreenModel : IDisposable {
	private static readonly Log Log = Log.ForType<MainContentScreenModel>();

	public DatabasePage DatabasePage { get; }
	private DatabasePageModel DatabasePageModel { get; }

	public TrackingPage TrackingPage { get; }
	private TrackingPageModel TrackingPageModel { get; }

	public AttachmentsPage AttachmentsPage { get; }
	private AttachmentsPageModel AttachmentsPageModel { get; }

	public ViewerPage ViewerPage { get; }
	private ViewerPageModel ViewerPageModel { get; }

	public AdvancedPage AdvancedPage { get; }
	private AdvancedPageModel AdvancedPageModel { get; }

	public DebugPage? DebugPage { get; }

	#if DEBUG
	public bool HasDebugPage => true;
	private DebugPageModel DebugPageModel { get; }
	#else
		public bool HasDebugPage => false;
	#endif

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

		AttachmentsPageModel = new AttachmentsPageModel(db);
		AttachmentsPage = new AttachmentsPage { DataContext = AttachmentsPageModel };

		ViewerPageModel = new ViewerPageModel(window, db);
		ViewerPage = new ViewerPage { DataContext = ViewerPageModel };

		AdvancedPageModel = new AdvancedPageModel(window, db, serverManager);
		AdvancedPage = new AdvancedPage { DataContext = AdvancedPageModel };

		#if DEBUG
		DebugPageModel = new DebugPageModel(window, db);
		DebugPage = new DebugPage { DataContext = DebugPageModel };
		#else
			DebugPage = null;
		#endif

		StatusBarModel = new StatusBarModel(db.Statistics);

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
		AttachmentsPageModel.Dispose();
		ViewerPageModel.Dispose();
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
