using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DHT.Utils.Resources;

public sealed class ResourceLoader(Assembly assembly) {
	private Stream? TryGetEmbeddedStream(string filename) {
		Stream? stream = null;
		
		foreach (var embeddedName in assembly.GetManifestResourceNames()) {
			if (embeddedName.Replace('\\', '/') == filename) {
				stream = assembly.GetManifestResourceStream(embeddedName);
				break;
			}
		}

		return stream;
	}
	
	private Stream GetEmbeddedStream(string filename) {
		return TryGetEmbeddedStream(filename) ?? throw new ArgumentException("Missing embedded resource: " + filename);
	}

	private async Task<string> ReadTextAsync(Stream stream) {
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return await reader.ReadToEndAsync();
	}
	
	private async Task<byte[]> ReadBytesAsync(Stream stream) {
		using var memoryStream = new MemoryStream();
		await stream.CopyToAsync(memoryStream);
		return memoryStream.ToArray();
	}

	public async Task<string> ReadTextAsync(string filename) {
		return await ReadTextAsync(GetEmbeddedStream(filename));
	}
	
	public async Task<byte[]?> ReadBytesAsyncIfExists(string filename) {
		return TryGetEmbeddedStream(filename) is {} stream ? await ReadBytesAsync(stream) : null;
	}

	public async Task<string> ReadJoinedAsync(string path, char separator) {
		StringBuilder joined = new ();

		foreach (var embeddedName in assembly.GetManifestResourceNames()) {
			if (embeddedName.Replace('\\', '/').StartsWith(path)) {
				joined.Append(await ReadTextAsync(assembly.GetManifestResourceStream(embeddedName)!)).Append(separator);
			}
		}

		return joined.ToString(0, Math.Max(0, joined.Length - 1));
	}
}
