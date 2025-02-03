using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DHT.Utils.Resources;

public sealed class ResourceLoader(Assembly assembly) {
	private Stream? TryGetEmbeddedStream(string filename) {
		Stream? stream = null;
		
		foreach (string embeddedName in assembly.GetManifestResourceNames()) {
			if (embeddedName.Replace(oldChar: '\\', newChar: '/') == filename) {
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
	
	public async Task<string> ReadJoinedAsync(string path, char separator, string[] order) {
		List<(string, Stream)> resourceNames = [];
		
		foreach (string embeddedName in assembly.GetManifestResourceNames()) {
			string embeddedNameNormalized = embeddedName.Replace(oldChar: '\\', newChar: '/');
			if (embeddedNameNormalized.StartsWith(path)) {
				resourceNames.Add((embeddedNameNormalized, assembly.GetManifestResourceStream(embeddedName)!));
			}
		}
		
		StringBuilder joined = new ();
		
		int GetOrderKey(string name) {
			int key = Array.FindIndex(order, name.EndsWith);
			return key == -1 ? order.Length : key;
		}
		
		foreach ((_, Stream stream) in resourceNames.OrderBy(item => GetOrderKey(item.Item1))) {
			joined.Append(await ReadTextAsync(stream)).Append(separator);
		}
		
		return joined.ToString(startIndex: 0, Math.Max(val1: 0, joined.Length - 1));
	}
}
