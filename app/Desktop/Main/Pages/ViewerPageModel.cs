using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.File;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Desktop.Main.Controls;
using DHT.Desktop.Server;
using DHT.Server;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Export;
using static DHT.Desktop.Program;

namespace DHT.Desktop.Main.Pages;

sealed partial class ViewerPageModel : ObservableObject, IDisposable {
	public static readonly ConcurrentBag<string> TemporaryFiles = [];

	private static readonly FilePickerFileType[] ViewerFileTypes = [
		FileDialogs.CreateFilter("Discord History Viewer", ["html"])
	];

	public bool DatabaseToolFilterModeKeep { get; set; } = true;
	public bool DatabaseToolFilterModeRemove { get; set; } = false;

	[ObservableProperty]
	private bool hasFilters = false;

	public MessageFilterPanelModel FilterModel { get; }

	private readonly Window window;
	private readonly State state;

	[Obsolete("Designer")]
	public ViewerPageModel() : this(null!, State.Dummy) {}

	public ViewerPageModel(Window window, State state) {
		this.window = window;
		this.state = state;

		FilterModel = new MessageFilterPanelModel(window, state, "Will export");
		FilterModel.FilterPropertyChanged += OnFilterPropertyChanged;
	}

	public void Dispose() {
		FilterModel.Dispose();
	}

	private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e) {
		HasFilters = FilterModel.HasAnyFilters;
	}

	public async void OnClickOpenViewer() {
		try {
			string serverUrl = "http://127.0.0.1:" + ServerConfiguration.Port;
			string serverToken = ServerConfiguration.Token;
			
			Process.Start(new ProcessStartInfo(serverUrl + "/viewer?token=" + HttpUtility.UrlEncode(serverToken)) {
				UseShellExecute = true
			});
		} catch (Exception e) {
			await Dialog.ShowOk(window, "Open Viewer", "Could not open viewer: " + e.Message);
		}
	}

	private async Task WriteViewerFile(string path, string url, string token) {
		const string ArchiveTag = "/*[ARCHIVE]*/";

		string indexFile = await Resources.ReadTextAsync("Viewer/index.html");
		string viewerTemplate = indexFile.Replace("/*[SERVER_URL]*/", HttpUtility.JavaScriptStringEncode(url))
		                                 .Replace("/*[SERVER_TOKEN]*/", HttpUtility.JavaScriptStringEncode(token));

		int viewerArchiveTagStart = viewerTemplate.IndexOf(ArchiveTag);
		int viewerArchiveTagEnd = viewerArchiveTagStart + ArchiveTag.Length;

		string jsonTempFile = path + ".tmp";

		await using (var jsonStream = new FileStream(jsonTempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read)) {
			await ViewerJsonExport.Generate(jsonStream, state.Db, FilterModel.CreateFilter());

			char[] jsonBuffer = new char[Math.Min(32768, jsonStream.Position)];
			jsonStream.Position = 0;

			await using (var outputStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
			await using (var outputWriter = new StreamWriter(outputStream, Encoding.UTF8)) {
				await outputWriter.WriteAsync(viewerTemplate[..viewerArchiveTagStart]);

				using (var jsonReader = new StreamReader(jsonStream, Encoding.UTF8)) {
					int readBytes;
					while ((readBytes = await jsonReader.ReadAsync(jsonBuffer, 0, jsonBuffer.Length)) > 0) {
						string jsonChunk = new string(jsonBuffer, 0, readBytes);
						await outputWriter.WriteAsync(HttpUtility.JavaScriptStringEncode(jsonChunk));
					}
				}

				await outputWriter.WriteAsync(viewerTemplate[viewerArchiveTagEnd..]);
			}
		}

		File.Delete(jsonTempFile);
	}

	public async Task OnClickApplyFiltersToDatabase() {
		var filter = FilterModel.CreateFilter();
		var messageCount = await ProgressDialog.ShowIndeterminate(window, "Apply Filters", "Counting matching messages...", _ => state.Db.Messages.Count(filter));

		if (DatabaseToolFilterModeKeep) {
			if (DialogResult.YesNo.Yes == await Dialog.ShowYesNo(window, "Keep Matching Messages in This Database", messageCount.Pluralize("message") + " will be kept, and the rest will be removed from this database. This action cannot be undone. Proceed?")) {
				await ApplyFilterToDatabase(filter, FilterRemovalMode.KeepMatching);
			}
		}
		else if (DatabaseToolFilterModeRemove) {
			if (DialogResult.YesNo.Yes == await Dialog.ShowYesNo(window, "Remove Matching Messages in This Database", messageCount.Pluralize("message") + " will be removed from this database. This action cannot be undone. Proceed?")) {
				await ApplyFilterToDatabase(filter, FilterRemovalMode.RemoveMatching);
			}
		}
	}

	private async Task ApplyFilterToDatabase(MessageFilter filter, FilterRemovalMode removalMode) {
		await ProgressDialog.ShowIndeterminate(window, "Apply Filters", "Removing messages...", _ => state.Db.Messages.Remove(filter, removalMode));
	}
}
