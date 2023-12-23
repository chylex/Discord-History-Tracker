using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DHT.Utils.Logging; 

[SupportedOSPlatform("windows")]
public static partial class WindowsConsole {
	[LibraryImport("kernel32.dll", SetLastError = true)]
	public static partial void AllocConsole();

	[LibraryImport("kernel32.dll", SetLastError = true)]
	public static partial void FreeConsole();
}
