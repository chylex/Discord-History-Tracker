using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs;
using DHT.Desktop.Models;
using DHT.Server.Database;
using DHT.Server.Logging;
using DHT.Server.Service;

namespace DHT.Desktop.Main.Pages {
	public class DatabasePageModel : BaseModel {
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

		public async void MergeWithDatabase() {
			var fileDialog = DatabaseGui.NewOpenDatabaseFileDialog();
			fileDialog.Directory = Path.GetDirectoryName(Db.Path);
			fileDialog.AllowMultiple = true;

			string[] paths = await fileDialog.ShowAsync(window);
			if (paths == null || paths.Length == 0) {
				return;
			}

			ProgressDialog progressDialog = new ProgressDialog();
			progressDialog.DataContext = new ProgressDialogModel(async callback => await MergeWithDatabaseFromPaths(Db, paths, progressDialog, callback)) {
				Title = "Database Merge"
			};

			await progressDialog.ShowDialog(window);
		}

		public void CloseDatabase() {
			ServerLauncher.Stop();
			DatabaseClosed?.Invoke(this, EventArgs.Empty);
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

			var oldStatistics = target.Statistics.Clone();
			int successful = 0;
			int finished = 0;

			foreach (string path in paths) {
				await callback.Update(Path.GetFileName(path), finished, total);
				++finished;

				if (!File.Exists(path)) {
					await Dialog.ShowOk(dialog, "Database Error", "Database '" + Path.GetFileName(path) + "' no longer exists.");
					continue;
				}

				IDatabaseFile? db = await DatabaseGui.TryOpenOrCreateDatabaseFromPath(path, dialog, CheckCanUpgradeDatabase);
				if (db == null) {
					continue;
				}

				try {
					target.AddFrom(db);
				} catch (Exception ex) {
					Log.Error(ex);
					await Dialog.ShowOk(dialog, "Database Error", "Database '" + Path.GetFileName(path) + "' could not be merged: " + ex.Message);
					continue;
				} finally {
					db.Dispose();
				}

				++successful;
			}

			await callback.Update("Done", finished, total);

			if (successful == 0) {
				await Dialog.ShowOk(dialog, "Database Merge", "Nothing was merged.");
				return;
			}

			var newStatistics = target.Statistics;
			long newServers = newStatistics.TotalServers - oldStatistics.TotalServers;
			long newChannels = newStatistics.TotalChannels - oldStatistics.TotalChannels;
			long newMessages = newStatistics.TotalMessages - oldStatistics.TotalMessages;

			string Pluralize(long count, string text) {
				return count + "\u00A0" + (count == 1 ? text : text + "s");
			}

			StringBuilder message = new StringBuilder();
			message.Append("Processed ");

			if (successful == total) {
				message.Append(Pluralize(successful, "database file"));
			}
			else {
				message.Append(successful).Append(" out of ").Append(Pluralize(total, "database file"));
			}

			message.Append(" and added:\n\n  \u2022 ");
			message.Append(Pluralize(newServers, "server")).Append("\n  \u2022 ");
			message.Append(Pluralize(newChannels, "channel")).Append("\n  \u2022 ");
			message.Append(Pluralize(newMessages, "message"));

			await Dialog.ShowOk(dialog, "Database Merge", message.ToString());
		}
	}
}
