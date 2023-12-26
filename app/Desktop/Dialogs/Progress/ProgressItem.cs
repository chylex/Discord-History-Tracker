using DHT.Utils.Models;

namespace DHT.Desktop.Dialogs.Progress;

sealed class ProgressItem : BaseModel {
	private bool isVisible = false;

	public bool IsVisible {
		get => isVisible;
		private set {
			Change(ref isVisible, value);
			OnPropertyChanged(nameof(Opacity));
		}
	}
	
	public double Opacity => IsVisible ? 1.0 : 0.0;

	private string message = "";

	public string Message {
		get => message;
		set {
			Change(ref message, value);
			IsVisible = !string.IsNullOrEmpty(value);
		}
	}

	private string items = "";

	public string Items {
		get => items;
		set => Change(ref items, value);
	}

	private int progress = 0;

	public int Progress {
		get => progress;
		set => Change(ref progress, value);
	}
}
