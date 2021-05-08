using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database;
using DHT.Server.Json;
using DHT.Server.Service;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints {
	public class TrackUsersEndpoint : BaseEndpoint {
		public TrackUsersEndpoint(IDatabaseFile db, ServerParameters parameters) : base(db, parameters) {}

		protected override async Task<(HttpStatusCode, object?)> Respond(HttpContext ctx) {
			var root = await ReadJson(ctx);

			if (root.ValueKind != JsonValueKind.Array) {
				throw new HttpException(HttpStatusCode.BadRequest, "Expected root element to be an array.");
			}

			User[] users = new User[root.GetArrayLength()];
			int i = 0;

			foreach (JsonElement user in root.EnumerateArray()) {
				users[i++] = ReadUser(user, "user");
			}

			Db.AddUsers(users);

			return (HttpStatusCode.OK, null);
		}

		private static User ReadUser(JsonElement json, string path) => new() {
			Id = json.RequireSnowflake("id", path),
			Name = json.RequireString("name", path),
			AvatarUrl = json.HasKey("avatar") ? json.RequireString("avatar", path) : null,
			Discriminator = json.HasKey("discriminator") ? json.RequireString("discriminator", path) : null
		};
	}
}
