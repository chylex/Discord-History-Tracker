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
using DHT.Server.Database.Sqlite.Schema;
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
				Process.Start("xdg-open", [folder]);
				break;
			
			case PlatformID.MacOSX:
				Process.Start("open", [folder]);
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
		string[] paths = await DatabaseGui.NewOpenDatabaseFilesDialog(window, Path.GetDirectoryName(Db.Path));
		if (paths.Length == 0) {
			return;
		}
		
		const string Title = "Database Merge";
		
		var result = new TaskCompletionSource<ImportResult?>();
		try {
			var dialog = new ProgressDialog();
			dialog.DataContext = new ProgressDialogModel(Title, async callbacks => result.SetResult(await MergeWithDatabaseFromPaths(Db, paths, dialog, callbacks)), progressItems: 2);
			await dialog.ShowProgressDialog(window);
		} catch (Exception e) {
			Log.Error("Could not merge databases.", e);
			await Dialog.ShowOk(window, Title, "Could not merge databases: " + e.Message);
			return;
		}
		
		await Dialog.ShowOk(window, Title, GetImportDialogMessage(result.Task.Result, "database file"));
	}
	
	private static async Task<ImportResult?> MergeWithDatabaseFromPaths(IDatabaseFile target, string[] paths, ProgressDialog dialog, IReadOnlyList<IProgressCallback> callbacks) {
		var schemaUpgradeCallbacks = new SchemaUpgradeCallbacks(dialog, callbacks[1], paths.Length);
		var databaseMergeProgressCallback = new DatabaseMergeProgressCallback(callbacks[1]);
		
		return await PerformImport(target, paths, dialog, callbacks[0], "Database Merge", async path => {
			IDatabaseFile? db = await DatabaseGui.TryOpenOrCreateDatabaseFromPath(path, dialog, schemaUpgradeCallbacks);
			
			if (db == null) {
				return false;
			}
			
			try {
				await target.Merge(db, databaseMergeProgressCallback);
				return true;
			} finally {
				await db.DisposeAsync();
			}
		});
	}
	
	private sealed class SchemaUpgradeCallbacks(ProgressDialog dialog, IProgressCallback callback, int total) : ISchemaUpgradeCallbacks {
		private bool? decision;
		
		public Task<InitialDatabaseSettings?> GetInitialDatabaseSettings() {
			return Task.FromResult<InitialDatabaseSettings?>(null);
		}
		
		public async Task<bool> CanUpgrade() {
			return decision ??= (total > 1
				                     ? await DatabaseGui.ShowCanUpgradeMultipleDatabaseDialog(dialog)
				                     : await DatabaseGui.ShowCanUpgradeDatabaseDialog(dialog)) == DialogResult.YesNo.Yes;
		}
		
		public Task Start(int versionSteps, Func<ISchemaUpgradeCallbacks.IProgressReporter, Task> doUpgrade) {
			callback.UpdateIndeterminate("Upgrading database...");
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
	
	private sealed class DatabaseMergeProgressCallback(IProgressCallback callback) : DatabaseMerging.IProgressCallback {
		public void OnImportingMetadata() {
			callback.UpdateIndeterminate("Importing metadata...");
		}
		
		public void OnMessagesImported(long finished, long total) {
			callback.Update("Importing messages...", finished, total);
		}
		
		public void OnDownloadsImported(long finished, long total) {
			callback.Update("Importing downloaded files...", finished, total);
		}
	}
	
	public async Task ImportLegacyArchive() {
		string[] paths = await window.StorageProvider.OpenFiles(new FilePickerOpenOptions {
			Title = "Open Legacy DHT Archive",
			SuggestedStartLocation = await FileDialogs.GetSuggestedStartLocation(window, Path.GetDirectoryName(Db.Path)),
			AllowMultiple = true,
		});
		
		if (paths.Length == 0) {
			return;
		}
		
		const string Title = "Legacy Archive Import";
		
		ImportResult? result;
		try {
			result = await ProgressDialog.Show(window, Title, async (dialog, callback) => await ImportLegacyArchiveFromPaths(Db, paths, dialog, callback));
		} catch (Exception e) {
			Log.Error("Could not import legacy archives.", e);
			await Dialog.ShowOk(window, Title, "Could not import legacy archives: " + e.Message);
			return;
		}
		
		await Dialog.ShowOk(window, Title, GetImportDialogMessage(result, "archive file"));
	}
	
	private static async Task<ImportResult?> ImportLegacyArchiveFromPaths(IDatabaseFile target, string[] paths, ProgressDialog dialog, IProgressCallback callback) {
		var fakeSnowflake = new FakeSnowflake();
		
		return await PerformImport(target, paths, dialog, callback, "Legacy Archive Import", async path => {
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
		
		foreach (DHT.Server.Data.Server server in servers.OrderBy(static server => server.Type).ThenBy(static server => server.Name)) {
			items.Add(new TextBoxItem<DHT.Server.Data.Server>(server) {
				Title = server.Name + " (" + ServerTypes.ToNiceString(server.Type) + ")",
				ValidityCheck = IsValidSnowflake,
			});
		}
		
		var model = new TextBoxDialogModel<DHT.Server.Data.Server>(items) {
			Title = "Imported Server IDs",
			Description = "Please fill in the IDs of servers and direct messages. First enable Developer Mode in Discord, then right-click each server or direct message, click 'Copy ID', and paste it into the input field. If a server no longer exists, leave its input field empty to use a random ID.",
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
	
	private static async Task<ImportResult?> PerformImport(IDatabaseFile target, string[] paths, ProgressDialog dialog, IProgressCallback callback, string dialogTitle, Func<string, Task<bool>> performImport) {
		int total = paths.Length;
		DatabaseStatistics oldStatistics = await DatabaseStatistics.Take(target);
		
		int successful = 0;
		int finished = 0;
		
		foreach (string path in paths) {
			await callback.Update("File: " + Path.GetFileName(path), finished, total);
			++finished;
			
			if (!File.Exists(path)) {
				await Dialog.ShowOk(dialog, dialogTitle, "File '" + Path.GetFileName(path) + "' no longer exists.");
				continue;
			}
			
			try {
				if (await performImport(path)) {
					++successful;
				}
			} catch (Exception ex) {
				Log.Error("Could not import file: " + path, ex);
				await Dialog.ShowOk(dialog, dialogTitle, "File '" + Path.GetFileName(path) + "' could not be imported: " + ex.Message);
			}
		}
		
		await callback.Update("Done", finished, total);
		
		if (successful == 0) {
			return null;
		}
		
		DatabaseStatistics newStatistics = await DatabaseStatistics.Take(target);
		return new ImportResult(oldStatistics, newStatistics, successful, total);
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
	
	private sealed record ImportResult(DatabaseStatistics OldStatistics, DatabaseStatistics NewStatistics, int SuccessfulItems, int TotalItems);
	
	private static string GetImportDialogMessage(ImportResult? result, string itemName) {
		if (result == null) {
			return "Nothing was imported.";
		}
		
		var oldStatistics = result.OldStatistics;
		var newStatistics = result.NewStatistics;
		
		long newServers = newStatistics.ServerCount - oldStatistics.ServerCount;
		long newChannels = newStatistics.ChannelCount - oldStatistics.ChannelCount;
		long newUsers = newStatistics.UserCount - oldStatistics.UserCount;
		long newMessages = newStatistics.MessageCount - oldStatistics.MessageCount;
		
		var message = new StringBuilder();
		message.Append("Processed ");
		
		if (result.SuccessfulItems == result.TotalItems) {
			message.Append(result.SuccessfulItems.Pluralize(itemName));
		}
		else {
			message.Append(result.SuccessfulItems.Format()).Append(" out of ").Append(result.TotalItems.Pluralize(itemName));
		}
		
		message.Append(" and added:\n\n  \u2022 ");
		message.Append(newServers.Pluralize("server")).Append("\n  \u2022 ");
		message.Append(newChannels.Pluralize("channel")).Append("\n  \u2022 ");
		message.Append(newUsers.Pluralize("user")).Append("\n  \u2022 ");
		message.Append(newMessages.Pluralize("message"));
		
		return message.ToString();
	}
	
	public async Task VacuumDatabase() {
		const string Title = "Vacuum Database";
		
		try {
			await ProgressDialog.ShowIndeterminate(window, Title, "Vacuuming database...", _ => Db.Vacuum());
		} catch (Exception e) {
			Log.Error("Could not vacuum database.", e);
			await Dialog.ShowOk(window, Title, "Could not vacuum database: " + e.Message);
			return;
		}
		
		await Dialog.ShowOk(window, Title, "Done.");
	}
}
