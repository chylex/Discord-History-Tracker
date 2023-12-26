using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Main.Pages;
using DHT.Utils.Logging;
using JetBrains.Annotations;

namespace DHT.Desktop.Main;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class MainWindow : Window {
	private static readonly Log Log = Log.ForType<MainWindow>();
	
	[UsedImplicitly]
	public MainWindow() {
		InitializeComponent();
		DataContext = new MainWindowModel(this, Arguments.Empty);
	}

	internal MainWindow(Arguments args) {
		InitializeComponent();
		DataContext = new MainWindowModel(this, args);
	}

	public async void OnClosing(object? sender, WindowClosingEventArgs e) {
		e.Cancel = true;
		Closing -= OnClosing;

		try {
			await Dispose();
		} finally {
			Close();
		}
	}

	private async Task Dispose() {
		if (DataContext is MainWindowModel model) {
			try {
				await model.DisposeAsync();
			} catch (Exception ex) {
				Log.Error("Caught exception while disposing window: " + ex);
			}
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
