using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DHT.Desktop.Dialogs.Message;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class MessageDialog : Window {
	public MessageDialog() {
		InitializeComponent();
	}

	public void ClickOk(object? sender, RoutedEventArgs e) {
		Close(DialogResult.All.Ok);
	}

	public void ClickYes(object? sender, RoutedEventArgs e) {
		Close(DialogResult.All.Yes);
	}

	public void ClickNo(object? sender, RoutedEventArgs e) {
		Close(DialogResult.All.No);
	}

	public void ClickCancel(object? sender, RoutedEventArgs e) {
		Close(DialogResult.All.Cancel);
	}
}
