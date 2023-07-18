using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DHT.Utils.Resources;

public sealed class ResourceLoader {
	private readonly Assembly assembly;

	public ResourceLoader(Assembly assembly) {
		this.assembly = assembly;
	}

	private Stream GetEmbeddedStream(string filename) {
		Stream? stream = null;
		foreach (var embeddedName in assembly.GetManifestResourceNames()) {
			if (embeddedName.Replace('\\', '/') == filename) {
				stream = assembly.GetManifestResourceStream(embeddedName);
				break;
			}
		}

		return stream ?? throw new ArgumentException("Missing embedded resource: " + filename);
	}

	private async Task<string> ReadTextAsync(Stream stream) {
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return await reader.ReadToEndAsync();
	}

	public async Task<string> ReadTextAsync(string filename) {
		return await ReadTextAsync(GetEmbeddedStream(filename));
	}

	public async Task<string> ReadJoinedAsync(string path, char separator) {
		StringBuilder joined = new();

		foreach (var embeddedName in assembly.GetManifestResourceNames()) {
			if (embeddedName.Replace('\\', '/').StartsWith(path)) {
				joined.Append(await ReadTextAsync(assembly.GetManifestResourceStream(embeddedName)!)).Append(separator);
			}
		}

		return joined.ToString(0, Math.Max(0, joined.Length - 1));
	}
}
