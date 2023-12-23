using System;
using System.Globalization;
using System.Reflection;
using Avalonia;
using DHT.Utils.Logging;
using DHT.Utils.Resources;

namespace DHT.Desktop;

static class Program {
	public static string Version { get; }
	public static CultureInfo Culture { get; }
	public static ResourceLoader Resources { get; }
	public static Arguments Arguments { get; }

	static Program() {
		var assembly = Assembly.GetExecutingAssembly();

		Version = assembly.GetName().Version?.ToString() ?? "";
		while (Version.EndsWith(".0")) {
			Version = Version[..^2];
		}

		Culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

		Resources = new ResourceLoader(assembly);
		Arguments = new Arguments(Environment.GetCommandLineArgs());
	}

	public static void Main(string[] args) {
		if (Arguments.Console && OperatingSystem.IsWindows()) {
			WindowsConsole.AllocConsole();
		}

		try {
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		} finally {
			if (Arguments.Console && OperatingSystem.IsWindows()) {
				WindowsConsole.FreeConsole();
			}
		}
	}

	private static AppBuilder BuildAvaloniaApp() {
		return AppBuilder.Configure<App>()
		                 .UsePlatformDetect()
		                 .WithInterFont()
		                 .LogToTrace();
	}
}
