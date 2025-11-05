using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Service.Middlewares;
using DHT.Utils.Resources;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

[ServerAuthorizationMiddleware.NoAuthorization]
sealed class GetUserscriptEndpoint(ResourceLoader resources) : BaseEndpoint {
	protected override async Task Respond(HttpRequest request, HttpResponse response, CancellationToken cancellationToken) {
		const string FileName = "dht.user.js";
		const string ResourcePath = "Tracker/loader/" + FileName;
		
		byte[]? resourceBytes = await resources.ReadBytesAsyncIfExists(ResourcePath);
		await WriteFileIfFound(response, FileName, resourceBytes, cancellationToken);
	}
}
