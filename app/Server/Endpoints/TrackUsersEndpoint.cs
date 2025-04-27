using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database;
using DHT.Utils.Http;
using Sisk.Core.Http;

namespace DHT.Server.Endpoints;

sealed class TrackUsersEndpoint(IDatabaseFile db) : BaseEndpoint {
	protected override async Task<HttpResponse> Respond(HttpRequest request) {
		JsonElement root = await ReadJson(request);
		
		if (root.ValueKind != JsonValueKind.Array) {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected root element to be an array.");
		}
		
		var users = new User[root.GetArrayLength()];
		int i = 0;
		
		foreach (JsonElement user in root.EnumerateArray()) {
			users[i++] = ReadUser(user, "user");
		}
		
		await db.Users.Add(users);
		
		return new HttpResponse();
	}
	
	private static User ReadUser(JsonElement json, string path) {
		return new User  {
			Id = json.RequireSnowflake("id", path),
			Name = json.RequireString("name", path),
			DisplayName = json.HasKey("displayName") ? json.RequireString("displayName", path) : null,
			AvatarHash = json.HasKey("avatar") ? json.RequireString("avatar", path) : null,
			Discriminator = json.HasKey("discriminator") ? json.RequireString("discriminator", path) : null,
		};
	}
}
