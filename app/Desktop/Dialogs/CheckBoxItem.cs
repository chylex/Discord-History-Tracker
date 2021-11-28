using DHT.Desktop.Models;

namespace DHT.Desktop.Dialogs {
	public class CheckBoxItem : BaseModel {
		public string Title { get; init; } = "";
		public object? Item { get; init; } = null;

		private bool isChecked = false;

		public bool Checked {
			get => isChecked;
			set => Change(ref isChecked, value);
		}
	}

	public class CheckBoxItem<T> : CheckBoxItem {
		public new T Item { get; }

		public CheckBoxItem(T item) {
			this.Item = item;
			base.Item = item;
		}
	}
}
