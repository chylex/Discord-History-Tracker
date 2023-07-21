using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DHT.Desktop.App.Windows;

namespace DHT.Desktop.App;

sealed class App : Application {
	public override void Initialize() {
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.MainWindow = new MainWindow(new Arguments(desktop.Args ?? Array.Empty<string>()));
		}

		base.OnFrameworkInitializationCompleted();
	}
}
