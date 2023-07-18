using System;
using DHT.Server.Database;
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Controls;

sealed class StatusBarModel : BaseModel {
	public DatabaseStatistics DatabaseStatistics { get; }

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

	[Obsolete("Designer")]
	public StatusBarModel() : this(new DatabaseStatistics()) {}

	public StatusBarModel(DatabaseStatistics databaseStatistics) {
		this.DatabaseStatistics = databaseStatistics;
	}

	public enum Status {
		Starting,
		Ready,
		Stopping,
		Stopped
	}
}
