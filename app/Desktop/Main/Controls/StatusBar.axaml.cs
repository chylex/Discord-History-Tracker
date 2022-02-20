using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main.Controls {
	sealed class StatusBar : UserControl {
		public StatusBar() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}
