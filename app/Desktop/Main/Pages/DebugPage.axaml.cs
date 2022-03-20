using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main.Pages {
	[SuppressMessage("ReSharper", "MemberCanBeInternal")]
	public sealed class DebugPage : UserControl {
		public DebugPage() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}
