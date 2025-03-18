using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Main.Dialogs;

sealed partial class NewDatabaseSettingsDialogModel {
	[Notify]
	private bool separateFileForDownloads = true;
	
	[Notify]
	private bool downloadsAutoStart = true;
}
