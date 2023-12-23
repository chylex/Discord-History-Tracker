using System.Text;
using System.Text.Json.Serialization.Metadata;
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

	public sealed class Text : IHttpOutput {
		private readonly string text;

		public Text(string text) {
			this.text = text;
		}

		public Task WriteTo(HttpResponse response) {
			return response.WriteAsync(text, Encoding.UTF8);
		}
	}

	public sealed class Json<TValue> : IHttpOutput {
		private readonly TValue value;
		private readonly JsonTypeInfo<TValue> typeInfo;

		public Json(TValue value, JsonTypeInfo<TValue> typeInfo) {
			this.value = value;
			this.typeInfo = typeInfo;
		}

		public Task WriteTo(HttpResponse response) {
			return response.WriteAsJsonAsync(value, typeInfo);
		}
	}

	public sealed class File : IHttpOutput {
		private readonly string? contentType;
		private readonly byte[] bytes;

		public File(string? contentType, byte[] bytes) {
			this.contentType = contentType;
			this.bytes = bytes;
		}

		public async Task WriteTo(HttpResponse response) {
			response.ContentType = contentType ?? string.Empty;
			await response.Body.WriteAsync(bytes);
		}
	}

	public sealed class Redirect : IHttpOutput {
		private readonly string url;
		private readonly bool permanent;

		public Redirect(string url, bool permanent) {
			this.url = url;
			this.permanent = permanent;
		}

		public Task WriteTo(HttpResponse response) {
			response.Redirect(url, permanent);
			return Task.CompletedTask;
		}
	}
}
