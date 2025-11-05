using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DHT.Desktop.Main.Dialogs;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class NewDatabaseSettingsDialog : Window {
	public NewDatabaseSettingsDialog() {
		InitializeComponent();
	}
	
	private void OnClosing(object? sender, WindowClosingEventArgs e) {
		if (!e.IsProgrammatic) {
			e.Cancel = true;
		}
	}
	
	private void ClickOk(object? sender, RoutedEventArgs e) {
		Close();
	}
}
