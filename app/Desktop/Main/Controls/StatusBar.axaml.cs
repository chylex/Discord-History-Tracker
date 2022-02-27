using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main.Controls {
	[SuppressMessage("ReSharper", "MemberCanBeInternal")]
	public sealed class StatusBar : UserControl {
		public StatusBar() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}
