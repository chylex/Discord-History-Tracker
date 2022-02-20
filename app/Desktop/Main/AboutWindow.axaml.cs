using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main {
	sealed class AboutWindow : Window {
		public AboutWindow() {
			InitializeComponent();
			#if DEBUG
			this.AttachDevTools();
			#endif
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}
