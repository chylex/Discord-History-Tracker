using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using DHT.Desktop.Models;
using DHT.Desktop.Resources;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Server.Database.Export;

namespace DHT.Desktop.Main.Pages {
	public class ViewerPageModel : BaseModel {
		public string ExportedMessageText { get; private set; } = "";

		private bool filterByDate = false;

		public bool FilterByDate {
			get => filterByDate;
			set => Change(ref filterByDate, value);
		}

		private DateTime? startDate = null;

		public DateTime? StartDate {
			get => startDate;
			set => Change(ref startDate, value);
		}

		private DateTime? endDate = null;

		public DateTime? EndDate {
			get => endDate;
			set => Change(ref endDate, value);
		}

		private readonly Window window;
		private readonly IDatabaseFile db;

		[Obsolete("Designer")]
		public ViewerPageModel() : this(null!, DummyDatabaseFile.Instance) {}

		public ViewerPageModel(Window window, IDatabaseFile db) {
			this.window = window;
			this.db = db;

			this.PropertyChanged += OnPropertyChanged;
			this.db.Statistics.PropertyChanged += OnDbStatisticsChanged;
			UpdateStatistics();
		}

		private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName is nameof(FilterByDate) or nameof(StartDate) or nameof(EndDate)) {
				UpdateStatistics();
			}
		}

		private void OnDbStatisticsChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(DatabaseStatistics.TotalMessages)) {
				UpdateStatistics();
			}
		}

		private MessageFilter CreateFilter() {
			MessageFilter filter = new();

			if (FilterByDate) {
				filter.StartDate = StartDate;
				filter.EndDate = EndDate;
			}

			return filter;
		}

		private void UpdateStatistics() {
			ExportedMessageText = "Will export " + db.CountMessages(CreateFilter()) + " out of " + db.Statistics.TotalMessages + " message(s).";
			OnPropertyChanged(nameof(ExportedMessageText));
		}

		private async Task<string> GenerateViewerContents() {
			string json = ViewerJsonExport.Generate(db, CreateFilter());

			string index = await ResourceLoader.ReadTextAsync("Viewer/index.html");
			string viewer = index.Replace("/*[JS]*/", await ResourceLoader.ReadJoinedAsync("Viewer/scripts/", '\n'))
			                     .Replace("/*[CSS]*/", await ResourceLoader.ReadJoinedAsync("Viewer/styles/", '\n'))
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
	}
}
