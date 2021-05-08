using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DHT.Server.Database {
	public class DatabaseStatistics : INotifyPropertyChanged {
		private long totalServers;
		private long totalChannels;
		private long totalMessages;

		public long TotalServers {
			get => totalServers;
			internal set => Change(out totalServers, value);
		}

		public long TotalChannels {
			get => totalChannels;
			internal set => Change(out totalChannels, value);
		}

		public long TotalMessages {
			get => totalMessages;
			internal set => Change(out totalMessages, value);
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		private void Change<T>(out T field, T value, [CallerMemberName] string? propertyName = null) {
			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
