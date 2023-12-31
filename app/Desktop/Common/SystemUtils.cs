using System.Diagnostics;

namespace DHT.Desktop.Common;

static class SystemUtils {
	public static void OpenUrl(string url) {
		Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
	}
}
