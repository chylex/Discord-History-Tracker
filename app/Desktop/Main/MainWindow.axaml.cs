using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Controls;
using DHT.Desktop.Main.Pages;
using JetBrains.Annotations;

namespace DHT.Desktop.Main;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class MainWindow : Window {
	[UsedImplicitly]
	public MainWindow() {
		InitializeComponent();
		DataContext = new MainWindowModel(this, Arguments.Empty);
	}

	internal MainWindow(Arguments args) {
		InitializeComponent();
		DataContext = new MainWindowModel(this, args);
	}

	public async void OnClosed(object? sender, EventArgs e) {
		if (DataContext is MainWindowModel model) {
			await model.DisposeAsync();
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
