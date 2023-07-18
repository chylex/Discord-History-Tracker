using System.Diagnostics;

namespace DHT.Desktop.Main;

sealed class AboutWindowModel {
	public void ShowOfficialWebsite() {
		OpenUrl("https://dht.chylex.com");
	}

	public void ShowSourceCode() {
		OpenUrl("https://github.com/chylex/Discord-History-Tracker");
	}

	public void ShowLibraryAvalonia() {
		OpenUrl("https://www.nuget.org/packages/Avalonia");
	}

	public void ShowLibrarySqlite() {
		OpenUrl("https://www.sqlite.org");
	}

	public void ShowLibrarySqliteAdoNet() {
		OpenUrl("https://www.nuget.org/packages/Microsoft.Data.Sqlite");
	}

	public void ShowLibraryNetCore() {
		OpenUrl("https://github.com/dotnet/core");
	}

	private static void OpenUrl(string url) {
		Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
	}
}
