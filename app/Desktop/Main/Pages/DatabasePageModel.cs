using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.File;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Desktop.Dialogs.TextBox;
using DHT.Server;
using DHT.Server.Data;
using DHT.Server.Database;
using DHT.Server.Database.Import;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Utils.Logging;

namespace DHT.Desktop.Main.Pages;

sealed class DatabasePageModel {
	private static readonly Log Log = Log.ForType<DatabasePageModel>();

	public IDatabaseFile Db { get; }

	public event EventHandler? DatabaseClosed;

	private readonly Window window;

	[Obsolete("Designer")]
	public DatabasePageModel() : this(null!, State.Dummy) {}

	public DatabasePageModel(Window window, State state) {
		this.window = window;
		this.Db = state.Db;
	}

	public async Task OpenDatabaseFolder() {
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

	public async Task MergeWithDatabase() {
		var paths = await DatabaseGui.NewOpenDatabaseFilesDialog(window, Path.GetDirectoryName(Db.Path));
		if (paths.Length > 0) {
			await ProgressDialog.Show(window, "Database Merge", async (dialog, callback) => await MergeWithDatabaseFromPaths(Db, paths, dialog, callback));
		}
	}

	private static async Task MergeWithDatabaseFromPaths(IDatabaseFile target, string[] paths, ProgressDialog dialog, IProgressCallback callback) {
		var schemaUpgradeCallbacks = new SchemaUpgradeCallbacks(dialog, paths.Length);

		await PerformImport(target, paths, dialog, callback, "Database Merge", "Database Error", "database file", async path => {
			IDatabaseFile? db = await DatabaseGui.TryOpenOrCreateDatabaseFromPath(path, dialog, schemaUpgradeCallbacks);

			if (db == null) {
				return false;
			}

			try {
				await target.AddFrom(db);
				return true;
			} finally {
				await db.DisposeAsync();
			}
		});
	}

	private sealed class SchemaUpgradeCallbacks : ISchemaUpgradeCallbacks {
		private readonly ProgressDialog dialog;
		private readonly int total;
		private bool? decision;

		public SchemaUpgradeCallbacks(ProgressDialog dialog, int total) {
			this.total = total;
			this.dialog = dialog;
		}

		public async Task<bool> CanUpgrade() {
			return decision ??= (total > 1
				                     ? await DatabaseGui.ShowCanUpgradeMultipleDatabaseDialog(dialog)
				                     : await DatabaseGui.ShowCanUpgradeDatabaseDialog(dialog)) == DialogResult.YesNo.Yes;
		}

		public Task Start(int versionSteps, Func<ISchemaUpgradeCallbacks.IProgressReporter, Task> doUpgrade) {
			return doUpgrade(new NullReporter());
		}

		private sealed class NullReporter : ISchemaUpgradeCallbacks.IProgressReporter {
			public Task NextVersion() {
				return Task.CompletedTask;
			}

			public Task MainWork(string message, int finishedItems, int totalItems) {
				return Task.CompletedTask;
			}

			public Task SubWork(string message, int finishedItems, int totalItems) {
				return Task.CompletedTask;
			}
		}
	}

	public async Task ImportLegacyArchive() {
		var paths = await window.StorageProvider.OpenFiles(new FilePickerOpenOptions {
			Title = "Open Legacy DHT Archive",
			SuggestedStartLocation = await FileDialogs.GetSuggestedStartLocation(window, Path.GetDirectoryName(Db.Path)),
			AllowMultiple = true
		});

		if (paths.Length > 0) {
			await ProgressDialog.Show(window, "Legacy Archive Import", async (dialog, callback) => await ImportLegacyArchiveFromPaths(Db, paths, dialog, callback));
		}
	}

	private static async Task ImportLegacyArchiveFromPaths(IDatabaseFile target, string[] paths, ProgressDialog dialog, IProgressCallback callback) {
		var fakeSnowflake = new FakeSnowflake();

		await PerformImport(target, paths, dialog, callback, "Legacy Archive Import", "Legacy Archive Error", "archive file", async path => {
			await using var jsonStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

			return await LegacyArchiveImport.Read(jsonStream, target, fakeSnowflake, async servers => {
				SynchronizationContext? prevSyncContext = SynchronizationContext.Current;
				SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
				Dictionary<DHT.Server.Data.Server, ulong>? result = await Dispatcher.UIThread.InvokeAsync(() => AskForServerIds(dialog, servers));
				SynchronizationContext.SetSynchronizationContext(prevSyncContext);
				return result;
			});
		});
	}

	private static async Task<Dictionary<DHT.Server.Data.Server, ulong>?> AskForServerIds(Window window, DHT.Server.Data.Server[] servers) {
		static bool IsValidSnowflake(string value) {
			return string.IsNullOrEmpty(value) || ulong.TryParse(value, out _);
		}

		var items = new List<TextBoxItem<DHT.Server.Data.Server>>();

		foreach (var server in servers.OrderBy(static server => server.Type).ThenBy(static server => server.Name)) {
			items.Add(new TextBoxItem<DHT.Server.Data.Server>(server) {
				Title = server.Name + " (" + ServerTypes.ToNiceString(server.Type) + ")",
				ValidityCheck = IsValidSnowflake
			});
		}

		var model = new TextBoxDialogModel<DHT.Server.Data.Server>(items) {
			Title = "Imported Server IDs",
			Description = "Please fill in the IDs of servers and direct messages. First enable Developer Mode in Discord, then right-click each server or direct message, click 'Copy ID', and paste it into the input field. If a server no longer exists, leave its input field empty to use a random ID."
		};

		var dialog = new TextBoxDialog { DataContext = model };
		var result = await dialog.ShowDialog<DialogResult.OkCancel>(window);

		if (result != DialogResult.OkCancel.Ok) {
			return null;
		}

		return model.ValidItems
		            .Where(static item => !string.IsNullOrEmpty(item.Value))
		            .ToDictionary(static item => item.Item, static item => ulong.Parse(item.Value));
	}

	private static async Task PerformImport(IDatabaseFile target, string[] paths, ProgressDialog dialog, IProgressCallback callback, string neutralDialogTitle, string errorDialogTitle, string itemName, Func<string, Task<bool>> performImport) {
		int total = paths.Length;
		var oldStatistics = await DatabaseStatistics.Take(target);

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

		var newStatistics = await DatabaseStatistics.Take(target);
		await Dialog.ShowOk(dialog, neutralDialogTitle, GetImportDialogMessage(oldStatistics, newStatistics, successful, total, itemName));
	}

	private sealed record DatabaseStatistics(long ServerCount, long ChannelCount, long UserCount, long MessageCount) {
		public static async Task<DatabaseStatistics> Take(IDatabaseFile db) {
			return new DatabaseStatistics(
				await db.Servers.Count(),
				await db.Channels.Count(),
				await db.Users.Count(),
				await db.Messages.Count()
			);
		}
	}

	private static string GetImportDialogMessage(DatabaseStatistics oldStatistics, DatabaseStatistics newStatistics, int successfulItems, int totalItems, string itemName) {
		long newServers = newStatistics.ServerCount - oldStatistics.ServerCount;
		long newChannels = newStatistics.ChannelCount - oldStatistics.ChannelCount;
		long newUsers = newStatistics.UserCount - oldStatistics.UserCount;
		long newMessages = newStatistics.MessageCount - oldStatistics.MessageCount;

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
