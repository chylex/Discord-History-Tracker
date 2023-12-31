using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DHT.Desktop.Dialogs.CheckBox;

class CheckBoxDialogModel : ObservableObject {
	public string Title { get; init; } = "";

	private IReadOnlyList<CheckBoxItem> items = Array.Empty<CheckBoxItem>();

	public IReadOnlyList<CheckBoxItem> Items {
		get => items;

		protected set {
			foreach (var item in items) {
				item.PropertyChanged -= OnItemPropertyChanged;
			}

			items = value;

			foreach (var item in items) {
				item.PropertyChanged += OnItemPropertyChanged;
			}
		}
	}

	private bool pauseCheckEvents = false;

	public bool AreAllSelected => Items.All(static item => item.IsChecked);
	public bool AreNoneSelected => Items.All(static item => !item.IsChecked);

	public void SelectAll() => SetAllChecked(true);
	public void SelectNone() => SetAllChecked(false);

	private void SetAllChecked(bool isChecked) {
		pauseCheckEvents = true;

		foreach (var item in Items) {
			item.IsChecked = isChecked;
		}

		pauseCheckEvents = false;
		UpdateBulkButtons();
	}

	private void UpdateBulkButtons() {
		OnPropertyChanged(nameof(AreAllSelected));
		OnPropertyChanged(nameof(AreNoneSelected));
	}

	private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (!pauseCheckEvents && e.PropertyName == nameof(CheckBoxItem.IsChecked)) {
			UpdateBulkButtons();
		}
	}
}

sealed class CheckBoxDialogModel<T> : CheckBoxDialogModel {
	private new IReadOnlyList<CheckBoxItem<T>> Items { get; }

	public IEnumerable<CheckBoxItem<T>> SelectedItems => Items.Where(static item => item.IsChecked);

	public CheckBoxDialogModel(IEnumerable<CheckBoxItem<T>> items) {
		this.Items = new List<CheckBoxItem<T>>(items);
		base.Items = this.Items;
	}
}
