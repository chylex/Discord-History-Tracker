using DHT.Utils.Models;

namespace DHT.Desktop.App.Controls;

sealed class StatusBarModel : BaseModel {
	private Status status = Status.Stopped;

	public Status CurrentStatus {
		get => status;
		set {
			status = value;
			OnPropertyChanged(nameof(StatusText));
		}
	}

	public string StatusText {
		get {
			return CurrentStatus switch {
				Status.Starting => "STARTING",
				Status.Ready    => "READY",
				Status.Stopping => "STOPPING",
				Status.Stopped  => "STOPPED",
				_               => ""
			};
		}
	}

	public enum Status {
		Starting,
		Ready,
		Stopping,
		Stopped
	}
}
