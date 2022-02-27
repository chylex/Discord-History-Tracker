using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Main.Pages;
using DHT.Server.Database;
using DHT.Utils.Models;

namespace DHT.Desktop.Main {
	sealed class MainWindowModel : BaseModel {
		private const string DefaultTitle = "Discord History Tracker";

		public string Title { get; private set; } = DefaultTitle;

		public WelcomeScreen WelcomeScreen { get; }
		private WelcomeScreenModel WelcomeScreenModel { get; }

		public MainContentScreen? MainContentScreen { get; private set; }
		private MainContentScreenModel? MainContentScreenModel { get; set; }

		public bool ShowWelcomeScreen => db == null;
		public bool ShowMainContentScreen => db != null;

		private readonly Window window;

		private IDatabaseFile? db;

		[Obsolete("Designer")]
		public MainWindowModel() : this(null!, Arguments.Empty) {}

		public MainWindowModel(Window window, Arguments args) {
			this.window = window;

			WelcomeScreenModel = new WelcomeScreenModel(window);
			WelcomeScreen = new WelcomeScreen { DataContext = WelcomeScreenModel };

			WelcomeScreenModel.PropertyChanged += WelcomeScreenModelOnPropertyChanged;

			var dbFile = args.DatabaseFile;
			if (!string.IsNullOrWhiteSpace(dbFile)) {
				async void OnWindowOpened(object? o, EventArgs eventArgs) {
					window.Opened -= OnWindowOpened;

					// https://github.com/AvaloniaUI/Avalonia/issues/3071
					if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
						await Task.Delay(500);
					}

					if (File.Exists(dbFile)) {
						await WelcomeScreenModel.OpenOrCreateDatabaseFromPath(dbFile);
					}
					else {
						await Dialog.ShowOk(window, "Database Error", "Database file not found:\n" + dbFile);
					}
				}

				window.Opened += OnWindowOpened;
			}

			if (args.ServerPort != null) {
				TrackingPageModel.ServerPort = args.ServerPort.ToString()!;
			}

			if (args.ServerToken != null) {
				TrackingPageModel.ServerToken = args.ServerToken;
			}
		}

		private async void WelcomeScreenModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(WelcomeScreenModel.Db)) {
				if (MainContentScreenModel != null) {
					MainContentScreenModel.DatabaseClosed -= MainContentScreenModelOnDatabaseClosed;
					MainContentScreenModel.Dispose();
				}

				db?.Dispose();
				db = WelcomeScreenModel.Db;

				if (db == null) {
					Title = DefaultTitle;
					MainContentScreenModel = null;
					MainContentScreen = null;
				}
				else {
					Title = Path.GetFileName(db.Path) + " - " + DefaultTitle;
					MainContentScreenModel = new MainContentScreenModel(window, db);
					await MainContentScreenModel.Initialize();
					MainContentScreenModel.DatabaseClosed += MainContentScreenModelOnDatabaseClosed;
					MainContentScreen = new MainContentScreen { DataContext = MainContentScreenModel };
					OnPropertyChanged(nameof(MainContentScreen));
				}

				OnPropertyChanged(nameof(ShowWelcomeScreen));
				OnPropertyChanged(nameof(ShowMainContentScreen));
				OnPropertyChanged(nameof(Title));

				window.Focus();
			}
		}

		private void MainContentScreenModelOnDatabaseClosed(object? sender, EventArgs e) {
			WelcomeScreenModel.CloseDatabase();
		}
	}
}
