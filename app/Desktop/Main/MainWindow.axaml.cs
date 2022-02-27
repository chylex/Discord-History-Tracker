using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;

namespace DHT.Desktop.Main {
	[SuppressMessage("ReSharper", "MemberCanBeInternal")]
	public sealed class MainWindow : Window {
		[UsedImplicitly]
		public MainWindow() {
			InitializeComponent(Arguments.Empty);
		}

		internal MainWindow(Arguments args) {
			InitializeComponent(args);
		}

		private void InitializeComponent(Arguments args) {
			AvaloniaXamlLoader.Load(this);
			DataContext = new MainWindowModel(this, args);

			#if DEBUG
			this.AttachDevTools();
			#endif
		}
	}
}
