using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DHT.Desktop.Main;

namespace DHT.Desktop {
	public class App : Application {
		public override void Initialize() {
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted() {
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
				desktop.MainWindow = new MainWindow(new Arguments(desktop.Args));
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}
