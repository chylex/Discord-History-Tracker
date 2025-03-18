using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Dialogs.CheckBox;

partial class CheckBoxItem {
	public string Title { get; init; } = "";
	public object? Item { get; init; } = null;
	
	[Notify]
	private bool isChecked = false;
}

sealed class CheckBoxItem<T> : CheckBoxItem {
	public new T Item { get; }
	
	public CheckBoxItem(T item) {
		this.Item = item;
		base.Item = item;
	}
}
