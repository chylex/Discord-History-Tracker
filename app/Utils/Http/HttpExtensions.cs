using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DHT.Utils.Http;

public static class HttpExtensions {
	public static Task WriteTextAsync(this HttpResponse response, string text, CancellationToken cancellationToken) {
		return WriteTextAsync(response, MediaTypeNames.Text.Plain, text, cancellationToken);
	}
	
	public static async Task WriteTextAsync(this HttpResponse response, string contentType, string text, CancellationToken cancellationToken) {
		response.ContentType = contentType;
		await response.StartAsync(cancellationToken);
		await response.WriteAsync(text, Encoding.UTF8, cancellationToken);
	}
	
	public static async Task WriteFileAsync(this HttpResponse response, string? contentType, byte[] bytes, CancellationToken cancellationToken) {
		response.ContentType = contentType ?? string.Empty;
		response.ContentLength = bytes.Length;
		await response.StartAsync(cancellationToken);
		await response.Body.WriteAsync(bytes, cancellationToken);
	}
	
	public static async Task WriteStreamAsync(this HttpResponse response, string? contentType, ulong? contentLength, Stream source, CancellationToken cancellationToken) {
		response.ContentType = contentType ?? string.Empty;
		response.ContentLength = (long?) contentLength;
		await response.StartAsync(cancellationToken);
		await source.CopyToAsync(response.Body, cancellationToken);
	}
}
