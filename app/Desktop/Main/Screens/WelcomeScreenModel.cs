using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Server.Database;
using DHT.Server.Database.Sqlite;
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Screens;

sealed class WelcomeScreenModel : BaseModel, IDisposable {
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
		var path = await DatabaseGui.NewOpenOrCreateDatabaseFileDialog(window, Path.GetDirectoryName(dbFilePath));
		if (path != null) {
			await OpenOrCreateDatabaseFromPath(path);
		}
	}

	public async Task OpenOrCreateDatabaseFromPath(string path) {
		if (Db != null) {
			Db = null;
		}

		dbFilePath = path;
		Db = await DatabaseGui.TryOpenOrCreateDatabaseFromPath(path, window, new SchemaUpgradeCallbacks(window));

		OnPropertyChanged(nameof(Db));
		OnPropertyChanged(nameof(HasDatabase));
	}

	private sealed class SchemaUpgradeCallbacks : ISchemaUpgradeCallbacks {
		private readonly Window window;
		
		public SchemaUpgradeCallbacks(Window window) {
			this.window = window;
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

			await new ProgressDialog {
				DataContext = new ProgressDialogModel(StartUpgrade, progressItems: 3) {
					Title = "Upgrading Database"
				}
			}.ShowProgressDialog(window);
		}

		private sealed class ProgressReporter : ISchemaUpgradeCallbacks.IProgressReporter {
			private readonly IReadOnlyList<IProgressCallback> callbacks;
			
			private readonly int versionSteps;
			private int versionProgress = 0;
			
			public ProgressReporter(int versionSteps, IReadOnlyList<IProgressCallback> callbacks) {
				this.callbacks = callbacks;
				this.versionSteps = versionSteps;
			}

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

	public void CloseDatabase() {
		Dispose();
		OnPropertyChanged(nameof(Db));
		OnPropertyChanged(nameof(HasDatabase));
	}

	public async void ShowAboutDialog() {
		await new AboutWindow { DataContext = new AboutWindowModel() }.ShowDialog(this.window);
	}

	public void Exit() {
		window.Close();
	}

	public void Dispose() {
		Db?.Dispose();
		Db = null;
	}
}
