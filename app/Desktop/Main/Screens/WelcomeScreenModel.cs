using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Desktop.Main.Dialogs;
using DHT.Server.Database;
using DHT.Server.Database.Sqlite.Schema;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Utils.Logging;

namespace DHT.Desktop.Main.Screens;

sealed partial class WelcomeScreenModel : ObservableObject {
	private static readonly Log Log = Log.ForType<WelcomeScreenModel>();
	
	public string Version => Program.Version;
	
	[ObservableProperty(Setter = Access.Private)]
	private bool isOpenOrCreateDatabaseButtonEnabled = true;
	
	public event EventHandler<IDatabaseFile>? DatabaseSelected;
	
	private readonly Window window;
	
	private string? dbFilePath;
	
	[Obsolete("Designer")]
	public WelcomeScreenModel() : this(null!) {}
	
	public WelcomeScreenModel(Window window) {
		this.window = window;
	}
	
	public async Task OpenOrCreateDatabase() {
		IsOpenOrCreateDatabaseButtonEnabled = false;
		try {
			string? path = await DatabaseGui.NewOpenOrCreateDatabaseFileDialog(window, Path.GetDirectoryName(dbFilePath));
			if (path != null) {
				await OpenOrCreateDatabaseFromPath(path);
			}
		} finally {
			IsOpenOrCreateDatabaseButtonEnabled = true;
		}
	}
	
	public async Task OpenOrCreateDatabaseFromPath(string path) {
		dbFilePath = path;
		
		IDatabaseFile? db = await DatabaseGui.TryOpenOrCreateDatabaseFromPath(path, window, new SchemaUpgradeCallbacks(window));
		if (db != null) {
			DatabaseSelected?.Invoke(this, db);
		}
	}
	
	private sealed class SchemaUpgradeCallbacks(Window window) : ISchemaUpgradeCallbacks {
		public async Task<InitialDatabaseSettings?> GetInitialDatabaseSettings() {
			var model = new NewDatabaseSettingsDialogModel();
			
			var dialog = new NewDatabaseSettingsDialog { DataContext = model };
			await dialog.ShowDialog(window);
			
			return new InitialDatabaseSettings(model.SeparateFileForDownloads, model.DownloadsAutoStart);
		}
		
		public async Task<bool> CanUpgrade() {
			return DialogResult.YesNo.Yes == await DatabaseGui.ShowCanUpgradeDatabaseDialog(window);
		}
		
		public async Task Start(int versionSteps, Func<ISchemaUpgradeCallbacks.IProgressReporter, Task> doUpgrade) {
			async Task StartUpgrade(IReadOnlyList<IProgressCallback> callbacks) {
				var reporter = new ProgressReporter(versionSteps, callbacks);
				await reporter.NextVersion();
				await Task.Delay(TimeSpan.FromMilliseconds(800));
				await doUpgrade(reporter);
				await Task.Delay(TimeSpan.FromMilliseconds(600));
			}
			
			await new ProgressDialog { DataContext = new ProgressDialogModel("Upgrading Database", StartUpgrade, progressItems: 3) }.ShowProgressDialog(window);
		}
		
		private sealed class ProgressReporter(int versionSteps, IReadOnlyList<IProgressCallback> callbacks) : ISchemaUpgradeCallbacks.IProgressReporter {
			private int versionProgress = 0;
			
			public async Task NextVersion() {
				await callbacks[0].Update("Upgrading schema version...", versionProgress++, versionSteps);
				await HideChildren(0);
			}
			
			public async Task MainWork(string message, int finishedItems, int totalItems) {
				await callbacks[1].Update(message, finishedItems, totalItems);
				await HideChildren(1);
			}
			
			public async Task SubWork(string message, int finishedItems, int totalItems) {
				await callbacks[2].Update(message, finishedItems, totalItems);
				await HideChildren(2);
			}
			
			private async Task HideChildren(int parentIndex) {
				for (int i = parentIndex + 1; i < callbacks.Count; i++) {
					await callbacks[i].Hide();
				}
			}
		}
	}
	
	public async Task CheckUpdates() {
		var latestVersion = await ProgressDialog.ShowIndeterminate<Version?>(window, "Check Updates", "Checking for updates...", async _ => {
			var client = new HttpClient(new SocketsHttpHandler {
				AutomaticDecompression = DecompressionMethods.None,
				AllowAutoRedirect = false,
				UseCookies = false,
			});
			
			client.Timeout = TimeSpan.FromSeconds(30);
			client.MaxResponseContentBufferSize = 1024;
			client.DefaultRequestHeaders.UserAgent.ParseAdd("DiscordHistoryTracker/" + Program.Version);
			
			string response;
			try {
				response = await client.GetStringAsync(Program.Website + "/version");
			} catch (TaskCanceledException e) when (e.InnerException is TimeoutException) {
				await Dialog.ShowOk(window, "Check Updates", "Request timed out.");
				return null;
			} catch (Exception e) {
				Log.Error(e);
				await Dialog.ShowOk(window, "Check Updates", "Error checking for updates: " + e.Message);
				return null;
			}
			
			if (!System.Version.TryParse(response, out Version? latestVersion)) {
				await Dialog.ShowOk(window, "Check Updates", "Server returned an invalid response.");
				return null;
			}
			
			return latestVersion;
		});
		
		if (latestVersion == null) {
			return;
		}
		
		if (Program.AssemblyVersion >= latestVersion) {
			await Dialog.ShowOk(window, "Check Updates", "You are using the latest version.");
			return;
		}
		
		if (await Dialog.ShowYesNo(window, "Check Updates", "A newer version is available: v" + Program.VersionToString(latestVersion) + "\nVisit the official website and close the app?") == DialogResult.YesNo.Yes) {
			SystemUtils.OpenUrl(Program.Website);
			Exit();
		}
	}
	
	public async Task ShowAboutDialog() {
		await new AboutWindow { DataContext = new AboutWindowModel() }.ShowDialog(window);
	}
	
	public void Exit() {
		window.Close();
	}
}
