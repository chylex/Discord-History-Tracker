using System.Globalization;
using System.Reflection;
using Avalonia;

namespace DHT.Desktop {
	internal static class Program {
		public static string Version { get; }

		static Program() {
			Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
			while (Version.EndsWith(".0")) {
				Version = Version[..^2];
			}

			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
		}

		public static void Main(string[] args) {
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}

		private static AppBuilder BuildAvaloniaApp() {
			return AppBuilder.Configure<App>()
			                 .UsePlatformDetect()
			                 .LogToTrace();
		}
	}
}
