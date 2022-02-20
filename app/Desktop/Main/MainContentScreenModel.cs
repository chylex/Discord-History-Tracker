using System;
using Avalonia.Controls;
using DHT.Desktop.Main.Controls;
using DHT.Desktop.Main.Pages;
using DHT.Server.Database;
using DHT.Server.Service;

namespace DHT.Desktop.Main {
	sealed class MainContentScreenModel : IDisposable {
		public DatabasePage DatabasePage { get; }
		private DatabasePageModel DatabasePageModel { get; }

		public TrackingPage TrackingPage { get; }
		private TrackingPageModel TrackingPageModel { get; }

		public ViewerPage ViewerPage { get; }
		private ViewerPageModel ViewerPageModel { get; }

		public StatusBarModel StatusBarModel { get; }

		public event EventHandler? DatabaseClosed {
			add {
				DatabasePageModel.DatabaseClosed += value;
			}
			remove {
				DatabasePageModel.DatabaseClosed -= value;
			}
		}

		[Obsolete("Designer")]
		public MainContentScreenModel() : this(null!, DummyDatabaseFile.Instance) {}

		public MainContentScreenModel(Window window, IDatabaseFile db) {
			DatabasePageModel = new DatabasePageModel(window, db);
			DatabasePage = new DatabasePage { DataContext = DatabasePageModel };

			TrackingPageModel = new TrackingPageModel(window, db);
			TrackingPage = new TrackingPage { DataContext = TrackingPageModel };

			ViewerPageModel = new ViewerPageModel(window, db);
			ViewerPage = new ViewerPage { DataContext = ViewerPageModel };

			StatusBarModel = new StatusBarModel(db.Statistics);
			TrackingPageModel.ServerStatusChanged += TrackingPageModelOnServerStatusChanged;
			StatusBarModel.CurrentStatus = ServerLauncher.IsRunning ? StatusBarModel.Status.Ready : StatusBarModel.Status.Stopped;
		}

		public void Initialize() {
			TrackingPageModel.Initialize();
		}

		private void TrackingPageModelOnServerStatusChanged(object? sender, StatusBarModel.Status e) {
			StatusBarModel.CurrentStatus = e;
		}

		public void Dispose() {
			TrackingPageModel.Dispose();
		}
	}
}
