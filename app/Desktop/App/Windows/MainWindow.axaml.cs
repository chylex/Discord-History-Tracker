using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using JetBrains.Annotations;

namespace DHT.Desktop.App.Windows;

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

	public void OnClosed(object? sender, EventArgs e) {
		if (DataContext is IDisposable disposable) {
			disposable.Dispose();
		}
	}
}
