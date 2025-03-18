using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Dialogs.TextBox;

partial class TextBoxDialogModel {
	public string Title { get; init; } = "";
	public string Description { get; init; } = "";
	
	private IReadOnlyList<TextBoxItem> items = [];
	
	public IReadOnlyList<TextBoxItem> Items {
		get => items;
		
		protected set {
			foreach (TextBoxItem item in items) {
				item.ErrorsChanged -= OnItemErrorsChanged;
			}
			
			items = value;
			
			foreach (TextBoxItem item in items) {
				item.ErrorsChanged += OnItemErrorsChanged;
			}
		}
	}
	
	[DependsOn(nameof(Items))]
	public bool HasErrors => Items.Any(static item => !item.IsValid);
	
	private void OnItemErrorsChanged(object? sender, DataErrorsChangedEventArgs e) {
		OnPropertyChanged(new PropertyChangedEventArgs(nameof(Items)));
	}
}

sealed class TextBoxDialogModel<T> : TextBoxDialogModel {
	private new IReadOnlyList<TextBoxItem<T>> Items { get; }
	
	public IEnumerable<TextBoxItem<T>> ValidItems => Items.Where(static item => item.IsValid);
	
	public TextBoxDialogModel(IEnumerable<TextBoxItem<T>> items) {
		this.Items = new List<TextBoxItem<T>>(items);
		base.Items = this.Items;
	}
}
