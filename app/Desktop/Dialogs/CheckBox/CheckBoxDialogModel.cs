using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Dialogs.CheckBox;

partial class CheckBoxDialogModel {
	public string Title { get; init; } = "";
	
	private ImmutableArray<ICheckBoxItem> rootItems = [];
	
	public ImmutableArray<ICheckBoxItem> RootItems {
		get => rootItems;
		
		protected set {
			foreach (ICheckBoxItem item in ICheckBoxItem.GetAllRecursively(rootItems)) {
				item.PropertyChanged -= OnItemPropertyChanged;
			}
			
			rootItems = value;
			
			foreach (ICheckBoxItem item in ICheckBoxItem.GetAllRecursively(rootItems)) {
				item.PropertyChanged += OnItemPropertyChanged;
			}
		}
	}
	
	protected IEnumerable<ICheckBoxItem> AllItems => ICheckBoxItem.GetAllRecursively(RootItems);
	
	[DependsOn(nameof(RootItems))]
	public bool AreAllSelected => RootItems.All(static item => item.IsChecked == true);
	
	[DependsOn(nameof(RootItems))]
	public bool AreNoneSelected => RootItems.All(static item => item.IsChecked == false);
	
	private bool pauseUpdatingBulkButtons = false;
	
	public void SelectAll() => SetAllChecked(true);
	public void SelectNone() => SetAllChecked(false);
	
	private void SetAllChecked(bool isChecked) {
		pauseUpdatingBulkButtons = true;
		
		foreach (ICheckBoxItem item in RootItems) {
			item.IsChecked = isChecked;
		}
		
		pauseUpdatingBulkButtons = false;
		UpdateBulkButtons();
	}
	
	private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName == nameof(ICheckBoxItem.IsChecked) && !pauseUpdatingBulkButtons) {
			UpdateBulkButtons();
		}
	}
	
	private void UpdateBulkButtons() {
		OnPropertyChanged(new PropertyChangedEventArgs(nameof(RootItems)));
	}
}

sealed class CheckBoxDialogModel<T> : CheckBoxDialogModel {
	public IEnumerable<T> SelectedValues => AllItems.OfType<ICheckBoxItem.Leaf<T>>()
	                                                .Where(static item => item.IsChecked == true)
	                                                .Select(static item => item.Value);
	
	public CheckBoxDialogModel(ImmutableArray<ICheckBoxItem> items) {
		this.RootItems = items;
	}
}
