using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Dialogs.CheckBox;

partial class CheckBoxDialogModel {
	public string Title { get; init; } = "";
	
	private IReadOnlyList<CheckBoxItem> items = [];
	
	public IReadOnlyList<CheckBoxItem> Items {
		get => items;
		
		protected set {
			foreach (CheckBoxItem item in items) {
				item.PropertyChanged -= OnItemPropertyChanged;
			}
			
			items = value;
			
			foreach (CheckBoxItem item in items) {
				item.PropertyChanged += OnItemPropertyChanged;
			}
		}
	}
	
	private bool pauseCheckEvents = false;
	
	[DependsOn(nameof(Items))]
	public bool AreAllSelected => Items.All(static item => item.IsChecked);
	
	[DependsOn(nameof(Items))]
	public bool AreNoneSelected => Items.All(static item => !item.IsChecked);
	
	public void SelectAll() => SetAllChecked(true);
	public void SelectNone() => SetAllChecked(false);
	
	private void SetAllChecked(bool isChecked) {
		pauseCheckEvents = true;
		
		foreach (CheckBoxItem item in Items) {
			item.IsChecked = isChecked;
		}
		
		pauseCheckEvents = false;
		UpdateBulkButtons();
	}
	
	private void UpdateBulkButtons() {
		OnPropertyChanged(new PropertyChangedEventArgs(nameof(Items)));
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
		base.Items = Items;
	}
}
