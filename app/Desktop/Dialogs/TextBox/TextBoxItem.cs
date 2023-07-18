using System;
using System.Collections;
using System.ComponentModel;
using DHT.Utils.Models;

namespace DHT.Desktop.Dialogs.TextBox; 

class TextBoxItem : BaseModel, INotifyDataErrorInfo {
	public string Title { get; init; } = "";
	public object? Item { get; init; } = null;
		
	public Func<string, bool> ValidityCheck { get; init; } = static _ => true;
	public bool IsValid => ValidityCheck(Value);

	private string value = string.Empty;

	public string Value {
		get => this.value;
		set {
			Change(ref this.value, value);
			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
		}
	}

	public IEnumerable GetErrors(string? propertyName) {
		if (propertyName == nameof(Value) && !IsValid) {
			yield return string.Empty;
		}
	}

	public bool HasErrors => !IsValid;
	public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
}

sealed class TextBoxItem<T> : TextBoxItem {
	public new T Item { get; }

	public TextBoxItem(T item) {
		this.Item = item;
		base.Item = item;
	}
}
