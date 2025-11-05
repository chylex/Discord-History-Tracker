using DHT.Desktop.Common;

namespace DHT.Desktop.Main;

sealed class AboutWindowModel {
	public void OpenOfficialWebsite() {
		SystemUtils.OpenUrl(Program.Website);
	}
	
	public void OpenIssueTracker() {
		SystemUtils.OpenUrl("https://github.com/chylex/Discord-History-Tracker/issues");
	}
	
	public void OpenSourceCode() {
		SystemUtils.OpenUrl("https://github.com/chylex/Discord-History-Tracker");
	}
	
	public void OpenThirdPartyNetCore() {
		SystemUtils.OpenUrl("https://github.com/dotnet/core");
	}
	
	public void OpenThirdPartyAvalonia() {
		SystemUtils.OpenUrl("https://github.com/AvaloniaUI/Avalonia");
	}
	
	public void OpenThirdPartyPropertyChangedSourceGenerator() {
		SystemUtils.OpenUrl("https://github.com/canton7/PropertyChanged.SourceGenerator");
	}
	
	public void OpenThirdPartySqlite() {
		SystemUtils.OpenUrl("https://www.sqlite.org");
	}
	
	public void OpenThirdPartyMicrosoftDataSqlite() {
		SystemUtils.OpenUrl("https://www.nuget.org/packages/Microsoft.Data.Sqlite");
	}
	
	public void OpenThirdPartyRxNet() {
		SystemUtils.OpenUrl("https://github.com/dotnet/reactive");
	}
	
	public void OpenThirdPartyBetterDiscord() {
		SystemUtils.OpenUrl("https://github.com/BetterDiscord/BetterDiscord");
	}
}
