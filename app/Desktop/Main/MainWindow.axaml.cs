using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DHT.Desktop.Main.Pages;
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

		public void OnClosed(object? sender, EventArgs e) {
			if (DataContext is IDisposable disposable) {
				disposable.Dispose();
			}

			foreach (var temporaryFile in ViewerPageModel.TemporaryFiles) {
				try {
					File.Delete(temporaryFile);
				} catch (Exception) {
					// ignored
				}
			}
		}
	}
}
