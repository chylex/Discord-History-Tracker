using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main {
	sealed class MainContentScreen : UserControl {
		public MainContentScreen() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}
