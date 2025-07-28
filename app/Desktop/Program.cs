using System;
using System.Globalization;
using System.Reflection;
using Avalonia;
using DHT.Desktop.Common;
using DHT.Utils.Logging;
using DHT.Utils.Resources;

namespace DHT.Desktop;

static class Program {
	public static string Version { get; }
	public static Version AssemblyVersion { get; }
	public static CultureInfo Culture { get; }
	public static ResourceLoader Resources { get; }
	public static Arguments Arguments { get; }
	
	public const string Website = "https://dht.chylex.com";
	
	static Program() {
		var assembly = Assembly.GetExecutingAssembly();
		
		AssemblyVersion = assembly.GetName().Version ?? new Version(major: 0, minor: 0, build: 0, revision: 0);
		Version = VersionToString(AssemblyVersion);
		
		Culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
		
		Resources = new ResourceLoader(assembly);
		Arguments = new Arguments(Environment.GetCommandLineArgs());
	}
	
	public static string VersionToString(Version version) {
		string versionStr = version.ToString();
		
		while (versionStr.EndsWith(".0")) {
			versionStr = versionStr[..^2];
		}
		
		return versionStr;
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
		AvaloniaReflection.Check();
		
		return AppBuilder.Configure<App>()
		                 .UsePlatformDetect()
		                 .WithInterFont()
		                 .LogToTrace();
	}
}
