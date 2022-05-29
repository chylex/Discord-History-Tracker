using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Server.Database;
using DHT.Utils.Logging;
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Pages {
	sealed class DatabasePageModel : BaseModel {
		private static readonly Log Log = Log.ForType<DatabasePageModel>();

		public IDatabaseFile Db { get; }

		public event EventHandler? DatabaseClosed;

		private readonly Window window;

		[Obsolete("Designer")]
		public DatabasePageModel() : this(null!, DummyDatabaseFile.Instance) {}

		public DatabasePageModel(Window window, IDatabaseFile db) {
			this.window = window;
			this.Db = db;
		}

		public async void OpenDatabaseFolder() {
			string file = Db.Path;
			string? folder = Path.GetDirectoryName(file);

			if (folder == null) {
				return;
			}

			switch (Environment.OSVersion.Platform) {
				case PlatformID.Win32NT:
					Process.Start("explorer.exe", "/select,\"" + file + "\"");
					break;

				case PlatformID.Unix:
					Process.Start("xdg-open", new string[] { folder });
					break;

				case PlatformID.MacOSX:
					Process.Start("open", new string[] { folder });
					break;

				default:
					await Dialog.ShowOk(window, "Feature Not Supported", "This feature is not supported for your operating system.");
					break;
			}
		}

		public void CloseDatabase() {
			DatabaseClosed?.Invoke(this, EventArgs.Empty);
		}

		public async void MergeWithDatabase() {
			var fileDialog = DatabaseGui.NewOpenDatabaseFileDialog();
			fileDialog.Directory = Path.GetDirectoryName(Db.Path);
			fileDialog.AllowMultiple = true;

			string[]? paths = await fileDialog.ShowAsync(window);
			if (paths == null || paths.Length == 0) {
				return;
			}

			ProgressDialog progressDialog = new ProgressDialog();
			progressDialog.DataContext = new ProgressDialogModel(async callback => await MergeWithDatabaseFromPaths(Db, paths, progressDialog, callback)) {
				Title = "Database Merge"
			};

			await progressDialog.ShowDialog(window);
		}

		private static async Task MergeWithDatabaseFromPaths(IDatabaseFile target, string[] paths, ProgressDialog dialog, IProgressCallback callback) {
			int total = paths.Length;

			DialogResult.YesNo? upgradeResult = null;

			async Task<bool> CheckCanUpgradeDatabase() {
				upgradeResult ??= total > 1
					                  ? await DatabaseGui.ShowCanUpgradeMultipleDatabaseDialog(dialog)
					                  : await DatabaseGui.ShowCanUpgradeDatabaseDialog(dialog);

				return DialogResult.YesNo.Yes == upgradeResult;
			}
			
			await PerformImport(target, paths, dialog, callback, "Database Merge", "Database Error", "database file", async path => {
				SynchronizationContext? prevSyncContext = SynchronizationContext.Current;
				SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
				IDatabaseFile? db = await DatabaseGui.TryOpenOrCreateDatabaseFromPath(path, dialog, CheckCanUpgradeDatabase);
				SynchronizationContext.SetSynchronizationContext(prevSyncContext);
				
				if (db == null) {
					return false;
				}

				try {
					target.AddFrom(db);
					return true;
				} finally {
					db.Dispose();
				}
			});
		}

		private static async Task PerformImport(IDatabaseFile target, string[] paths, ProgressDialog dialog, IProgressCallback callback, string neutralDialogTitle, string errorDialogTitle, string itemName, Func<string, Task<bool>> performImport) {
			int total = paths.Length;
			var oldStatistics = target.SnapshotStatistics();

			int successful = 0;
			int finished = 0;

			foreach (string path in paths) {
				await callback.Update(Path.GetFileName(path), finished, total);
				++finished;

				if (!File.Exists(path)) {
					await Dialog.ShowOk(dialog, errorDialogTitle, "File '" + Path.GetFileName(path) + "' no longer exists.");
					continue;
				}

				try {
					if (await performImport(path)) {
						++successful;
					}
				} catch (Exception ex) {
					Log.Error(ex);
					await Dialog.ShowOk(dialog, errorDialogTitle, "File '" + Path.GetFileName(path) + "' could not be imported: " + ex.Message);
				}
			}

			await callback.Update("Done", finished, total);

			if (successful == 0) {
				await Dialog.ShowOk(dialog, neutralDialogTitle, "Nothing was imported.");
				return;
			}

			await Dialog.ShowOk(dialog, neutralDialogTitle, GetImportDialogMessage(oldStatistics, target.SnapshotStatistics(), successful, total, itemName));
		}

		private static string GetImportDialogMessage(DatabaseStatisticsSnapshot oldStatistics, DatabaseStatisticsSnapshot newStatistics, int successfulItems, int totalItems, string itemName) {
			long newServers = newStatistics.TotalServers - oldStatistics.TotalServers;
			long newChannels = newStatistics.TotalChannels - oldStatistics.TotalChannels;
			long newUsers = newStatistics.TotalUsers - oldStatistics.TotalUsers;
			long newMessages = newStatistics.TotalMessages - oldStatistics.TotalMessages;

			StringBuilder message = new StringBuilder();
			message.Append("Processed ");

			if (successfulItems == totalItems) {
				message.Append(successfulItems.Pluralize(itemName));
			}
			else {
				message.Append(successfulItems.Format()).Append(" out of ").Append(totalItems.Pluralize(itemName));
			}

			message.Append(" and added:\n\n  \u2022 ");
			message.Append(newServers.Pluralize("server")).Append("\n  \u2022 ");
			message.Append(newChannels.Pluralize("channel")).Append("\n  \u2022 ");
			message.Append(newUsers.Pluralize("user")).Append("\n  \u2022 ");
			message.Append(newMessages.Pluralize("message"));

			return message.ToString();
		}
	}
}
