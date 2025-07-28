using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using DHT.Desktop.Common;
using DHT.Desktop.Dialogs.Message;

namespace DHT.Desktop.Dialogs.CheckBox;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class CheckBoxDialog : Window {
	public CheckBoxDialog() {
		InitializeComponent();
	}
	
	private void TreeViewOnContainerPrepared(object? sender, ContainerPreparedEventArgs e) {
		foreach (object? item in TreeView.Items) {
			if (item != null && TreeView.ContainerFromItem(item) is TreeViewItem treeViewItem) {
				treeViewItem.TemplateApplied += TreeViewItemOnTemplateApplied;
				treeViewItem.GotFocus += TreeViewItemOnGotFocus;
				treeViewItem.KeyDown += TreeViewItemOnKeyDown;
			}
		}
	}
	
	private void TreeViewItemOnTemplateApplied(object? sender, TemplateAppliedEventArgs e) {
		if (sender is TreeViewItem { HeaderPresenter: Interactive headerPresenter } ) {
			// Removes support for double-clicking to expand.
			AvaloniaReflection.GetEventHandler(headerPresenter, DoubleTappedEvent)?.Clear();
		}
	}
	
	private void TreeViewItemOnGotFocus(object? sender, GotFocusEventArgs e) {
		if (e.NavigationMethod == NavigationMethod.Tab && sender is TreeViewItem treeViewItem && TreeView.SelectedItem == null) {
			TreeView.SelectedItem = TreeView.ItemFromContainer(treeViewItem);
		}
	}
	
	private void TreeViewItemOnKeyDown(object? sender, KeyEventArgs e) {
		if (e.Key == Key.Space && TreeView.SelectedItem is ICheckBoxItem item) {
			item.IsChecked = item.IsChecked == false;
			e.Handled = true;
		}
	}
	
	public void ClickOk(object? sender, RoutedEventArgs e) {
		Close(DialogResult.OkCancel.Ok);
	}
	
	public void ClickCancel(object? sender, RoutedEventArgs e) {
		Close(DialogResult.OkCancel.Cancel);
	}
}
