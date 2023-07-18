using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DHT.Desktop.Dialogs.Message;

namespace DHT.Desktop.Dialogs.CheckBox;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class CheckBoxDialog : Window {
	public CheckBoxDialog() {
		InitializeComponent();
	}

	public void ClickOk(object? sender, RoutedEventArgs e) {
		Close(DialogResult.OkCancel.Ok);
	}

	public void ClickCancel(object? sender, RoutedEventArgs e) {
		Close(DialogResult.OkCancel.Cancel);
	}
}
