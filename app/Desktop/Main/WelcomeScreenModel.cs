using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs;
using DHT.Desktop.Models;
using DHT.Server.Database;
using DHT.Server.Database.Exceptions;
using DHT.Server.Database.Sqlite;
using DHT.Server.Logging;

namespace DHT.Desktop.Main {
	public class WelcomeScreenModel : BaseModel {
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
			var dialog = new SaveFileDialog {
				Title = "Open or Create Database File",
				InitialFileName = "archive.dht",
				Directory = Path.GetDirectoryName(dbFilePath),
				Filters = new List<FileDialogFilter> {
					new() {
						Name = "Discord History Tracker Database",
						Extensions = { "dht" }
					}
				}
			}.ShowAsync(window);

			string path = await dialog;
			if (!string.IsNullOrWhiteSpace(path)) {
				await OpenOrCreateDatabaseFromPath(path);
			}
		}

		public async Task OpenOrCreateDatabaseFromPath(string path) {
			if (Db != null) {
				Db = null;
			}

			dbFilePath = path;

			try {
				Db = await SqliteDatabaseFile.OpenOrCreate(path, CheckCanUpgradeDatabase);
			} catch (InvalidDatabaseVersionException ex) {
				await Dialog.ShowOk(window, "Database Error", "This database appears to be corrupted (invalid version: " + ex.Version + ").");
			} catch (DatabaseTooNewException ex) {
				await Dialog.ShowOk(window, "Database Error", "This database was opened in a newer version of DHT (database version " + ex.DatabaseVersion + ", app version " + ex.CurrentVersion + ").");
			} catch (Exception ex) {
				Log.Error(ex);
				await Dialog.ShowOk(window, "Database Error", ex.Message);
			}

			OnPropertyChanged(nameof(Db));
			OnPropertyChanged(nameof(HasDatabase));
		}

		private async Task<bool> CheckCanUpgradeDatabase() {
			return DialogResult.YesNo.Yes == await Dialog.ShowYesNo(window, "Database Upgrade", "This database was created with an older version of DHT. If you proceed, the database will be upgraded and will no longer open in previous versions of DHT. Do you want to proceed?");
		}

		public void CloseDatabase() {
			Db = null;

			OnPropertyChanged(nameof(Db));
			OnPropertyChanged(nameof(HasDatabase));
		}

		public async void ShowAboutDialog() {
			await new AboutWindow() { DataContext = new AboutWindowModel() }.ShowDialog(this.window);
		}

		public void Exit() {
			window.Close();
		}
	}
}
