using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main {
	[SuppressMessage("ReSharper", "MemberCanBeInternal")]
	public sealed class WelcomeScreen : UserControl {
		public WelcomeScreen() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}
