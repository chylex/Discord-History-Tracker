using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DHT.Utils.Models;

namespace DHT.Desktop.Dialogs.TextBox; 

class TextBoxDialogModel : BaseModel {
	public string Title { get; init; } = "";
	public string Description { get; init; } = "";

	private IReadOnlyList<TextBoxItem> items = Array.Empty<TextBoxItem>();

	public IReadOnlyList<TextBoxItem> Items {
		get => items;

		protected set {
			foreach (var item in items) {
				item.ErrorsChanged -= OnItemErrorsChanged;
			}

			items = value;

			foreach (var item in items) {
				item.ErrorsChanged += OnItemErrorsChanged;
			}
		}
	}
		
	public bool HasErrors => Items.Any(static item => !item.IsValid);

	private void OnItemErrorsChanged(object? sender, DataErrorsChangedEventArgs e) {
		OnPropertyChanged(nameof(HasErrors));
	}
}

sealed class TextBoxDialogModel<T> : TextBoxDialogModel {
	public new IReadOnlyList<TextBoxItem<T>> Items { get; }

	public IEnumerable<TextBoxItem<T>> ValidItems => Items.Where(static item => item.IsValid);

	public TextBoxDialogModel(IEnumerable<TextBoxItem<T>> items) {
		this.Items = new List<TextBoxItem<T>>(items);
		base.Items = this.Items;
	}
}
