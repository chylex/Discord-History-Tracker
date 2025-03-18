using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Dialogs.Progress;

sealed partial class ProgressItem {
	[Notify]
	private string message = "";
	
	[Notify]
	private string items = "";
	
	[Notify]
	private long progress = 0L;
	
	[Notify]
	private bool isIndeterminate;
	
	[DependsOn(nameof(Message))]
	public bool IsVisible => !string.IsNullOrEmpty(Message);
	
	[DependsOn(nameof(IsVisible))]
	public double Opacity => IsVisible ? 1.0 : 0.0;
}
