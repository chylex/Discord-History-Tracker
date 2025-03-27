using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class TrackChannelEndpoint(IDatabaseFile db) : BaseEndpoint {
	protected override async Task Respond(HttpRequest request, HttpResponse response, CancellationToken cancellationToken) {
		JsonElement root = await ReadJson(request);
		Data.Server server = ReadServer(root.RequireObject("server"), "server");
		Channel channel = ReadChannel(root.RequireObject("channel"), "channel", server.Id);
		
		await db.Servers.Add([server]);
		await db.Channels.Add([channel]);
	}
	
	private static Data.Server ReadServer(JsonElement json, string path) {
		return new Data.Server  {
			Id = json.RequireSnowflake("id", path),
			Name = json.RequireString("name", path),
			Type = ServerTypes.FromString(json.RequireString("type", path)) ?? throw new HttpException(HttpStatusCode.BadRequest, "Server type must be either 'SERVER', 'GROUP', or 'DM'."),
		};
	}
	
	private static Channel ReadChannel(JsonElement json, string path, ulong serverId) {
		return new Channel  {
			Id = json.RequireSnowflake("id", path),
			Server = serverId,
			Name = json.RequireString("name", path),
			ParentId = json.HasKey("parent") ? json.RequireSnowflake("parent", path) : null,
			Position = json.HasKey("position") ? json.RequireInt("position", path, min: 0) : null,
			Topic = json.HasKey("topic") ? json.RequireString("topic", path) : null,
			Nsfw = json.HasKey("nsfw") ? json.RequireBool("nsfw", path) : null,
		};
	}
}
