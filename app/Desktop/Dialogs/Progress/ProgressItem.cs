using CommunityToolkit.Mvvm.ComponentModel;

namespace DHT.Desktop.Dialogs.Progress;

sealed partial class ProgressItem : ObservableObject {
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Opacity))]
	private partial bool IsVisible { get; set; } = false;
	
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
	public partial string Items { get; set; } = "";
	
	[ObservableProperty]
	public partial int Progress { get; set; } = 0;
	
	[ObservableProperty]
	public partial bool IsIndeterminate { get; set; }
}
