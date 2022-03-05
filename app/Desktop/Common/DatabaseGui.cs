using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Server.Database;
using DHT.Server.Database.Exceptions;
using DHT.Server.Database.Sqlite;
using DHT.Utils.Logging;

namespace DHT.Desktop.Common {
	static class DatabaseGui {
		private static readonly Log Log = Log.ForType(typeof(DatabaseGui));

		private const string DatabaseFileInitialName = "archive.dht";

		private static readonly List<FileDialogFilter> DatabaseFileDialogFilter = new() {
			new FileDialogFilter {
				Name = "Discord History Tracker Database",
				Extensions = { "dht" }
			}
		};

		public static OpenFileDialog NewOpenDatabaseFileDialog() {
			return new OpenFileDialog {
				Title = "Open Database File",
				InitialFileName = DatabaseFileInitialName,
				Filters = DatabaseFileDialogFilter
			};
		}

		public static SaveFileDialog NewOpenOrCreateDatabaseFileDialog() {
			return new SaveFileDialog {
				Title = "Open or Create Database File",
				InitialFileName = DatabaseFileInitialName,
				Filters = DatabaseFileDialogFilter
			};
		}

		public static async Task<IDatabaseFile?> TryOpenOrCreateDatabaseFromPath(string path, Window window, Func<Task<bool>> checkCanUpgradeDatabase) {
			IDatabaseFile? file = null;

			try {
				file = await SqliteDatabaseFile.OpenOrCreate(path, checkCanUpgradeDatabase);
			} catch (InvalidDatabaseVersionException ex) {
				await Dialog.ShowOk(window, "Database Error", "Database '" + Path.GetFileName(path) + "' appears to be corrupted (invalid version: " + ex.Version + ").");
			} catch (DatabaseTooNewException ex) {
				await Dialog.ShowOk(window, "Database Error", "Database '" + Path.GetFileName(path) + "' was opened in a newer version of DHT (database version " + ex.DatabaseVersion + ", app version " + ex.CurrentVersion + ").");
			} catch (Exception ex) {
				Log.Error(ex);
				await Dialog.ShowOk(window, "Database Error", "Database '" + Path.GetFileName(path) + "' could not be opened:" + ex.Message);
			}

			return file;
		}

		public static async Task<DialogResult.YesNo> ShowCanUpgradeDatabaseDialog(Window window) {
			return await Dialog.ShowYesNo(window, "Database Upgrade", "This database was created with an older version of DHT. If you proceed, the database will be upgraded and will no longer open in previous versions of DHT.\n\nPlease ensure you have a backup of the database. Do you want to proceed with the upgrade?");
		}

		public static async Task<DialogResult.YesNo> ShowCanUpgradeMultipleDatabaseDialog(Window window) {
			return await Dialog.ShowYesNo(window, "Database Upgrade", "One or more databases were created with an older version of DHT. If you proceed, these databases will be upgraded and will no longer open in previous versions of DHT. Otherwise, these databases will be skipped.\n\nPlease ensure you have a backup of the databases. Do you want to proceed with the upgrade?");
		}
	}
}
