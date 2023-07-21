using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class TrackChannelEndpoint : BaseEndpoint {
	public TrackChannelEndpoint(IDatabaseFile db, ServerAccessToken accessToken) : base(db, accessToken) {}

	protected override async Task<IHttpOutput> Respond(HttpContext ctx) {
		var root = await ReadJson(ctx);
		
		return HttpOutput.None;
	}
}
