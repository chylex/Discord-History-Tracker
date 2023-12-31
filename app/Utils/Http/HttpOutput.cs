using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DHT.Utils.Http;

public static class HttpOutput {
	public static IHttpOutput None { get; } = new NoneImpl();

	private sealed class NoneImpl : IHttpOutput {
		public Task WriteTo(HttpResponse response) {
			return Task.CompletedTask;
		}
	}

	public sealed class Text(string text) : IHttpOutput {
		public Task WriteTo(HttpResponse response) {
			return response.WriteAsync(text, Encoding.UTF8);
		}
	}

	public sealed class File(string? contentType, byte[] bytes) : IHttpOutput {
		public async Task WriteTo(HttpResponse response) {
			response.ContentType = contentType ?? string.Empty;
			await response.Body.WriteAsync(bytes);
		}
	}

	public sealed class Redirect(string url, bool permanent) : IHttpOutput {
		public Task WriteTo(HttpResponse response) {
			response.Redirect(url, permanent);
			return Task.CompletedTask;
		}
	}
}
