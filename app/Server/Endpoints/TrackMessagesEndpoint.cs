using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class TrackMessagesEndpoint : BaseEndpoint {
	public TrackMessagesEndpoint(IDatabaseFile db, ServerAccessToken accessToken) : base(db, accessToken) {}

	protected override async Task<IHttpOutput> Respond(HttpContext ctx) {
		var root = await ReadJson(ctx);

		if (root.ValueKind != JsonValueKind.Array) {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected root element to be an array.");
		}

		return new HttpOutput.Json(0);
	}
}
