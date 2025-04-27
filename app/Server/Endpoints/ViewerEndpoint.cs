using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DHT.Utils.Resources;
using Sisk.Core.Http;

namespace DHT.Server.Endpoints;

// [ServerAuthorizationMiddleware.NoAuthorization]
sealed class ViewerEndpoint(ResourceLoader resources) : BaseEndpoint {
	private readonly Dictionary<string, byte[]?> cache = new ();
	private readonly SemaphoreSlim cacheSemaphore = new (1);
	
	protected override async Task<HttpResponse> Respond(HttpRequest request) {
		string path = request.RouteParameters.GetValue("path") ?? "index.html";
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
		
		return await WriteFileIfFound(path, resourceBytes);
	}
}
