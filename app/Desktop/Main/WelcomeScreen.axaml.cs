using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main {
	sealed class WelcomeScreen : UserControl {
		public WelcomeScreen() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}
