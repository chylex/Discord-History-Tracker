using DHT.Desktop.Common;

namespace DHT.Desktop.Main;

sealed class AboutWindowModel {
	public void ShowOfficialWebsite() {
		SystemUtils.OpenUrl(Program.Website);
	}
	
	public void ShowIssueTracker() {
		SystemUtils.OpenUrl("https://github.com/chylex/Discord-History-Tracker/issues");
	}
	
	public void ShowSourceCode() {
		SystemUtils.OpenUrl("https://github.com/chylex/Discord-History-Tracker");
	}
	
	public void ShowLibraryNetCore() {
		SystemUtils.OpenUrl("https://github.com/dotnet/core");
	}
	
	public void ShowLibraryAvalonia() {
		SystemUtils.OpenUrl("https://github.com/AvaloniaUI/Avalonia");
	}
	
	public void ShowLibraryPropertyChangedSourceGenerator() {
		SystemUtils.OpenUrl("https://github.com/canton7/PropertyChanged.SourceGenerator");
	}
	
	public void ShowLibrarySqlite() {
		SystemUtils.OpenUrl("https://www.sqlite.org");
	}
	
	public void ShowLibrarySqliteAdoNet() {
		SystemUtils.OpenUrl("https://www.nuget.org/packages/Microsoft.Data.Sqlite");
	}
	
	public void ShowLibraryRxNet() {
		SystemUtils.OpenUrl("https://github.com/dotnet/reactive");
	}
}
