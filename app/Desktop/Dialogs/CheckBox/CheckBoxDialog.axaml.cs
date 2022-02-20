using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DHT.Desktop.Dialogs.Message;

namespace DHT.Desktop.Dialogs.CheckBox {
	sealed class CheckBoxDialog : Window {
		public CheckBoxDialog() {
			InitializeComponent();
			#if DEBUG
			this.AttachDevTools();
			#endif
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		public void ClickOk(object? sender, RoutedEventArgs e) {
			Close(DialogResult.OkCancel.Ok);
		}

		public void ClickCancel(object? sender, RoutedEventArgs e) {
			Close(DialogResult.OkCancel.Cancel);
		}
	}
}

