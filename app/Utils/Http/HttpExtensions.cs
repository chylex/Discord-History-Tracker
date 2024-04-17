using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DHT.Utils.Http; 

public static class HttpExtensions {
	public static Task WriteTextAsync(this HttpResponse response, string text) {
		return WriteTextAsync(response, MediaTypeNames.Text.Plain, text);
	}
	
	public static async Task WriteTextAsync(this HttpResponse response, string contentType, string text) {
		response.ContentType = contentType;
		await response.StartAsync();
		await response.WriteAsync(text, Encoding.UTF8);
	}
	
	public static async Task WriteFileAsync(this HttpResponse response, string? contentType, byte[] bytes) {
		response.ContentType = contentType ?? string.Empty;
		response.ContentLength = bytes.Length;
		await response.StartAsync();
		await response.Body.WriteAsync(bytes);
	}
	
	public static async Task WriteStreamAsync(this HttpResponse response, string? contentType, ulong? contentLength, Stream source) {
		response.ContentType = contentType ?? string.Empty;
		response.ContentLength = (long?) contentLength;
		await response.StartAsync();
		await source.CopyToAsync(response.Body);
	}
}
