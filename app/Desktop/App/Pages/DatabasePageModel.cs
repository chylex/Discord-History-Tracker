using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using DHT.Desktop.App.Dialogs.Message;
using DHT.Server.Database;
using DHT.Utils.Logging;
using DHT.Utils.Models;

namespace DHT.Desktop.App.Pages; 

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
}
