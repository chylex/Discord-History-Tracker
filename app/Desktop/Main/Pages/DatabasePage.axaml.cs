using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main.Pages {
	sealed class DatabasePage : UserControl {
		public DatabasePage() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}
