using CommunityToolkit.Mvvm.ComponentModel;

namespace DHT.Desktop.Dialogs.Progress;

sealed partial class ProgressItem : ObservableObject {
	[ObservableProperty(Setter = Access.Private)]
	[NotifyPropertyChangedFor(nameof(Opacity))]
	private bool isVisible = false;
	
	public double Opacity => IsVisible ? 1.0 : 0.0;
	
	private string message = "";
	
	public string Message {
		get => message;
		set {
			SetProperty(ref message, value);
			IsVisible = !string.IsNullOrEmpty(value);
		}
	}
	
	[ObservableProperty]
	private string items = "";
	
	[ObservableProperty]
	private int progress = 0;
	
	[ObservableProperty]
	private bool isIndeterminate;
}
