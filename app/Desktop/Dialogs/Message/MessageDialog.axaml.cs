using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Dialogs.Message {
	public class MessageDialog : Window {
		public MessageDialog() {
			InitializeComponent();
			#if DEBUG
			this.AttachDevTools();
			#endif
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
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
}
