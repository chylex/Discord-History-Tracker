using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace DHT.Utils.Models;

public abstract class BaseModel : INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;

	[NotifyPropertyChangedInvocator]
	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected void Change<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null) {
		if (!EqualityComparer<T>.Default.Equals(field, newValue)) {
			field = newValue;
			OnPropertyChanged(propertyName);
		}
	}
}
