using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Main.Controls;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Server.Database.Export;
using DHT.Utils.Models;
using static DHT.Desktop.Program;

namespace DHT.Desktop.Main.Pages {
	sealed class ViewerPageModel : BaseModel, IDisposable {
		public string ExportedMessageText { get; private set; } = "";

		public bool DatabaseToolFilterModeKeep { get; set; } = true;
		public bool DatabaseToolFilterModeRemove { get; set; } = false;

		private bool hasFilters = false;

		public bool HasFilters {
			get => hasFilters;
			set => Change(ref hasFilters, value);
		}

		private FilterPanelModel FilterModel { get; }

		private readonly Window window;
		private readonly IDatabaseFile db;

		[Obsolete("Designer")]
		public ViewerPageModel() : this(null!, DummyDatabaseFile.Instance) {}

		public ViewerPageModel(Window window, IDatabaseFile db) {
			this.window = window;
			this.db = db;

			FilterModel = new FilterPanelModel(window, db);
			FilterModel.FilterPropertyChanged += OnFilterPropertyChanged;
			db.Statistics.PropertyChanged += OnDbStatisticsChanged;
			UpdateStatistics();
		}

		public void Dispose() {
			db.Statistics.PropertyChanged -= OnDbStatisticsChanged;
			FilterModel.Dispose();
		}

		private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e) {
			UpdateStatistics();
			HasFilters = FilterModel.HasAnyFilters;
		}

		private void OnDbStatisticsChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(DatabaseStatistics.TotalMessages)) {
				UpdateStatistics();
			}
		}

		private void UpdateStatistics() {
			ExportedMessageText = "Will export " + db.CountMessages(FilterModel.CreateFilter()).Format() + " out of " + db.Statistics.TotalMessages.Format() + " message(s).";
			OnPropertyChanged(nameof(ExportedMessageText));
		}

		private async Task<string> GenerateViewerContents() {
			string json = ViewerJsonExport.Generate(db, FilterModel.CreateFilter());
			
			string index = await Resources.ReadTextAsync("Viewer/index.html");
			string viewer = index.Replace("/*[JS]*/", await Resources.ReadJoinedAsync("Viewer/scripts/", '\n'))
			                     .Replace("/*[CSS]*/", await Resources.ReadJoinedAsync("Viewer/styles/", '\n'))
			                     .Replace("/*[ARCHIVE]*/", HttpUtility.JavaScriptStringEncode(json));
			return viewer;
		}

		public async void OnClickOpenViewer() {
			string rootPath = Path.Combine(Path.GetTempPath(), "DiscordHistoryTracker");
			string filenameBase = Path.GetFileNameWithoutExtension(db.Path) + "-" + DateTime.Now.ToString("yyyy-MM-dd");
			string fullPath = Path.Combine(rootPath, filenameBase + ".html");
			int counter = 0;

			while (File.Exists(fullPath)) {
				++counter;
				fullPath = Path.Combine(rootPath, filenameBase + "-" + counter + ".html");
			}

			Directory.CreateDirectory(rootPath);
			await File.WriteAllTextAsync(fullPath, await GenerateViewerContents());

			Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
		}

		public async void OnClickSaveViewer() {
			var dialog = new SaveFileDialog {
				Title = "Save Viewer",
				InitialFileName = "archive.html",
				Directory = Path.GetDirectoryName(db.Path),
				Filters = new List<FileDialogFilter> {
					new() {
						Name = "Discord History Viewer",
						Extensions = { "html" }
					}
				}
			}.ShowAsync(window);

			string? path = await dialog;
			if (!string.IsNullOrEmpty(path)) {
				await File.WriteAllTextAsync(path, await GenerateViewerContents());
			}
		}

		public async void OnClickApplyFiltersToDatabase() {
			var filter = FilterModel.CreateFilter();

			if (DatabaseToolFilterModeKeep) {
				if (DialogResult.YesNo.Yes == await Dialog.ShowYesNo(window, "Keep Matching Messages in This Database", db.CountMessages(filter).Pluralize("message") + " will be kept, and the rest will be removed from this database. This action cannot be undone. Proceed?")) {
					db.RemoveMessages(filter, MessageFilterRemovalMode.KeepMatching);
				}
			}
			else if (DatabaseToolFilterModeRemove) {
				if (DialogResult.YesNo.Yes == await Dialog.ShowYesNo(window, "Remove Matching Messages in This Database", db.CountMessages(filter).Pluralize("message") + " will be removed from this database. This action cannot be undone. Proceed?")) {
					db.RemoveMessages(filter, MessageFilterRemovalMode.RemoveMatching);
				}
			}
		}
	}
}
