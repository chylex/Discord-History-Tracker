using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class TrackChannelEndpoint : BaseEndpoint {
	public TrackChannelEndpoint(IDatabaseFile db, ServerParameters parameters) : base(db, parameters) {}

	protected override async Task<IHttpOutput> Respond(HttpContext ctx) {
		var root = await ReadJson(ctx);
		var server = ReadServer(root.RequireObject("server"), "server");
		var channel = ReadChannel(root.RequireObject("channel"), "channel", server.Id);

		Db.AddServer(server);
		Db.AddChannel(channel);

		return HttpOutput.None;
	}

	private static Data.Server ReadServer(JsonElement json, string path) => new() {
		Id = json.RequireSnowflake("id", path),
		Name = json.RequireString("name", path),
		Type = ServerTypes.FromString(json.RequireString("type", path)) ?? throw new HttpException(HttpStatusCode.BadRequest, "Server type must be either 'SERVER', 'GROUP', or 'DM'.")
	};

	private static Channel ReadChannel(JsonElement json, string path, ulong serverId) => new() {
		Id = json.RequireSnowflake("id", path),
		Server = serverId,
		Name = json.RequireString("name", path),
		ParentId = json.HasKey("parent") ? json.RequireSnowflake("parent", path) : null,
		Position = json.HasKey("position") ? json.RequireInt("position", path, min: 0) : null,
		Topic = json.HasKey("topic") ? json.RequireString("topic", path) : null,
		Nsfw = json.HasKey("nsfw") ? json.RequireBool("nsfw", path) : null
	};
}
