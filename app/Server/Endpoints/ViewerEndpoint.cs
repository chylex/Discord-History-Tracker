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

sealed class ViewerEndpoint : BaseEndpoint {
	private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new ();

	private readonly ResourceLoader resources;
	private readonly Dictionary<string, byte[]?> cache = new ();
	private readonly SemaphoreSlim cacheSemaphore = new (1);

	public ViewerEndpoint(IDatabaseFile db, ResourceLoader resources) : base(db) {
		this.resources = resources;
	}
	
	protected override async Task<IHttpOutput> Respond(HttpContext ctx) {
		string path = (string?) ctx.Request.RouteValues["path"] ?? "index.html";
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
			return new HttpOutput.File(contentType, resourceBytes);
		}
	}
}
