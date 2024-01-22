using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Utils.Http;
using DHT.Utils.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace DHT.Server.Endpoints;

sealed class ViewerEndpoint(IDatabaseFile db, ResourceLoader resources) : BaseEndpoint(db) {
	private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new ();

	private readonly Dictionary<string, byte[]?> cache = new ();
	private readonly SemaphoreSlim cacheSemaphore = new (1);

	protected override async Task Respond(HttpRequest request, HttpResponse response) {
		string path = (string?) request.RouteValues["path"] ?? "index.html";
		string resourcePath = "Viewer/" + path;
		
		byte[]? resourceBytes;

		await cacheSemaphore.WaitAsync();
		try {
			if (!cache.TryGetValue(resourcePath, out resourceBytes)) {
				cache[resourcePath] = resourceBytes = await resources.ReadBytesAsyncIfExists(resourcePath);
			}
		} finally {
			cacheSemaphore.Release();
		}

		if (resourceBytes == null) {
			throw new HttpException(HttpStatusCode.NotFound, "File not found: " + path);
		}
		else {
			var contentType = ContentTypeProvider.TryGetContentType(path, out string? type) ? type : null;
			await response.WriteFileAsync(contentType, resourceBytes);
		}
	}
}
