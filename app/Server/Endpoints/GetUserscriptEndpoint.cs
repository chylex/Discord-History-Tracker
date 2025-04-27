using System.Threading.Tasks;
using DHT.Utils.Resources;
using Sisk.Core.Http;

namespace DHT.Server.Endpoints;

// [ServerAuthorizationMiddleware.NoAuthorization]
sealed class GetUserscriptEndpoint(ResourceLoader resources) : BaseEndpoint {
	protected override async Task<HttpResponse> Respond(HttpRequest request) {
		const string FileName = "dht.user.js";
		const string ResourcePath = "Tracker/loader/" + FileName;
		
		byte[]? resourceBytes = await resources.ReadBytesAsyncIfExists(ResourcePath);
		return await WriteFileIfFound(FileName, resourceBytes);
	}
}
