using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DHT.Desktop.Resources {
	public static class ResourceLoader {
		private static Stream GetEmbeddedStream(string filename) {
			Stream? stream = null;
			Assembly assembly = Assembly.GetExecutingAssembly();
			foreach (var embeddedName in assembly.GetManifestResourceNames()) {
				if (embeddedName.Replace('\\', '/') == filename) {
					stream = assembly.GetManifestResourceStream(embeddedName);
					break;
				}
			}

			return stream ?? throw new ArgumentException("Missing embedded resource: " + filename);
		}

		private static async Task<string> ReadTextAsync(Stream stream) {
			using var reader = new StreamReader(stream, Encoding.UTF8);
			return await reader.ReadToEndAsync();
		}

		public static async Task<string> ReadTextAsync(string filename) {
			return await ReadTextAsync(GetEmbeddedStream(filename));
		}

		public static async Task<string> ReadJoinedAsync(string path, char separator) {
			StringBuilder joined = new();

			Assembly assembly = Assembly.GetExecutingAssembly();
			foreach (var embeddedName in assembly.GetManifestResourceNames()) {
				if (embeddedName.Replace('\\', '/').StartsWith(path)) {
					joined.Append(await ReadTextAsync(assembly.GetManifestResourceStream(embeddedName)!)).Append(separator);
				}
			}

			return joined.ToString(0, Math.Max(0, joined.Length - 1));
		}
	}
}
