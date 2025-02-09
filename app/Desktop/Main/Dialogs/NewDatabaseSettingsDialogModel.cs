using CommunityToolkit.Mvvm.ComponentModel;

namespace DHT.Desktop.Main.Dialogs;

sealed partial class NewDatabaseSettingsDialogModel : ObservableObject {
	[ObservableProperty]
	private bool separateFileForDownloads = true;
	
	[ObservableProperty]
	private bool downloadsAutoStart = true;
}
