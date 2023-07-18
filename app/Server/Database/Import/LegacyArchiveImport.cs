using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Utils.Collections;
using DHT.Utils.Http;
using DHT.Utils.Logging;
using Microsoft.AspNetCore.StaticFiles;

namespace DHT.Server.Database.Import;

public static class LegacyArchiveImport {
	private static readonly Log Log = Log.ForType(typeof(LegacyArchiveImport));

	private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new ();

	public static async Task<bool> Read(Stream stream, IDatabaseFile db, FakeSnowflake fakeSnowflake, Func<Data.Server[], Task<Dictionary<Data.Server, ulong>?>> askForServerIds) {
		var perf = Log.Start();
		var root = await JsonSerializer.DeserializeAsync<JsonElement>(stream);

		try {
			var meta = root.RequireObject("meta");
			var data = root.RequireObject("data");

			perf.Step("Deserialize JSON");

			var users = ReadUserList(meta);
			var servers = ReadServerList(meta, fakeSnowflake);

			var newServersOnly = new HashSet<Data.Server>(servers);
			var oldServersById = db.GetAllServers().ToDictionary(static server => server.Id, static server => server);

			var oldChannels = db.GetAllChannels();
			var oldChannelsById = oldChannels.ToDictionary(static channel => channel.Id, static channel => channel);

			foreach (var (channelId, serverIndex) in ReadChannelToServerIndexMapping(meta, servers)) {
				if (oldChannelsById.TryGetValue(channelId, out var oldChannel) && oldServersById.TryGetValue(oldChannel.Server, out var oldServer) && newServersOnly.Remove(servers[serverIndex])) {
					servers[serverIndex] = oldServer;
				}
			}

			perf.Step("Read server and user list");

			if (newServersOnly.Count > 0) {
				var askedServerIds = await askForServerIds(newServersOnly.ToArray());
				if (askedServerIds == null) {
					return false;
				}

				perf.Step("Ask for server IDs");

				for (var i = 0; i < servers.Length; i++) {
					var server = servers[i];
					if (askedServerIds.TryGetValue(server, out var serverId)) {
						servers[i] = server with { Id = serverId };
					}
				}
			}

			var channels = ReadChannelList(meta, servers);

			perf.Step("Read channel list");

			var oldMessageIds = db.GetMessageIds();
			var newMessages = channels.SelectMany(channel => ReadMessages(data, channel, users, fakeSnowflake))
			                          .Where(message => !oldMessageIds.Contains(message.Id))
			                          .ToArray();

			perf.Step("Read messages");

			db.AddUsers(users);
			db.AddServers(servers);
			db.AddChannels(channels);
			db.AddMessages(newMessages);

			perf.Step("Import into database");
		} catch (HttpException e) {
			throw new JsonException(e.Message);
		}

		perf.End();
		return true;
	}

	private static User[] ReadUserList(JsonElement meta) {
		const string UsersPath = "meta.users[]";

		static ulong ParseUserIndex(JsonElement element, int index) {
			return ulong.Parse(element.GetString() ?? throw new JsonException("Expected key 'meta.userindex[" + index + "]' to be a string."));
		}

		var userindex = meta.RequireArray("userindex", "meta")
		                    .Select(static (item, index) => (ParseUserIndex(item, index), index))
		                    .ToDictionary();

		var users = new User[userindex.Count];

		foreach (var item in meta.RequireObject("users", "meta").EnumerateObject()) {
			var path = UsersPath + "." + item.Name;
			var userId = ulong.Parse(item.Name);
			var userObj = item.Value;

			users[userindex[userId]] = new User {
				Id = userId,
				Name = userObj.RequireString("name", path),
				AvatarUrl = userObj.HasKey("avatar") ? userObj.RequireString("avatar", path) : null,
				Discriminator = userObj.HasKey("tag") ? userObj.RequireString("tag", path) : null,
			};
		}

		return users;
	}

	private static Data.Server[] ReadServerList(JsonElement meta, FakeSnowflake fakeSnowflake) {
		const string ServersPath = "meta.servers[]";

		return meta.RequireArray("servers", "meta").Select(serverObj => new Data.Server {
			Id = fakeSnowflake.Next(),
			Name = serverObj.RequireString("name", ServersPath),
			Type = ServerTypes.FromString(serverObj.RequireString("type", ServersPath)),
		}).ToArray();
	}

	private const string ChannelsPath = "meta.channels";

	private static Dictionary<ulong, int> ReadChannelToServerIndexMapping(JsonElement meta, Data.Server[] servers) {
		return meta.RequireObject("channels", "meta").EnumerateObject().Select(item => {
			var path = ChannelsPath + "." + item.Name;
			var channelId = ulong.Parse(item.Name);
			var channelObj = item.Value;

			return (channelId, channelObj.RequireInt("server", path, min: 0, max: servers.Length - 1));
		}).ToDictionary();
	}

	private static Channel[] ReadChannelList(JsonElement meta, Data.Server[] servers) {
		return meta.RequireObject("channels", "meta").EnumerateObject().Select(item => {
			var path = ChannelsPath + "." + item.Name;
			var channelId = ulong.Parse(item.Name);
			var channelObj = item.Value;

			return new Channel {
				Id = channelId,
				Server = servers[channelObj.RequireInt("server", path, min: 0, max: servers.Length - 1)].Id,
				Name = channelObj.RequireString("name", path),
				Position = channelObj.HasKey("position") ? channelObj.RequireInt("position", path, min: 0) : null,
				Topic = channelObj.HasKey("topic") ? channelObj.RequireString("topic", path) : null,
				Nsfw = channelObj.HasKey("nsfw") ? channelObj.RequireBool("nsfw", path) : null,
			};
		}).ToArray();
	}

	private static Message[] ReadMessages(JsonElement data, Channel channel, User[] users, FakeSnowflake fakeSnowflake) {
		const string DataPath = "data";

		var channelId = channel.Id;
		var channelIdStr = channelId.ToString();

		var messagesObj = data.HasKey(channelIdStr) ? data.RequireObject(channelIdStr, DataPath) : (JsonElement?) null;
		if (messagesObj == null) {
			return Array.Empty<Message>();
		}

		return messagesObj.Value.EnumerateObject().Select(item => {
			var path = DataPath + "." + item.Name;
			var messageId = ulong.Parse(item.Name);
			var messageObj = item.Value;

			return new Message {
				Id = messageId,
				Sender = users[messageObj.RequireInt("u", path, min: 0, max: users.Length - 1)].Id,
				Channel = channelId,
				Text = messageObj.HasKey("m") ? messageObj.RequireString("m", path) : string.Empty,
				Timestamp = messageObj.RequireLong("t", path),
				EditTimestamp = messageObj.HasKey("te") ? messageObj.RequireLong("te", path) : null,
				RepliedToId = messageObj.HasKey("r") ? messageObj.RequireSnowflake("r", path) : null,
				Attachments = messageObj.HasKey("a") ? ReadMessageAttachments(messageObj.RequireArray("a", path), fakeSnowflake, path + ".a[]").ToImmutableArray() : ImmutableArray<Attachment>.Empty,
				Embeds = messageObj.HasKey("e") ? ReadMessageEmbeds(messageObj.RequireArray("e", path), path + ".e[]").ToImmutableArray() : ImmutableArray<Embed>.Empty,
				Reactions = messageObj.HasKey("re") ? ReadMessageReactions(messageObj.RequireArray("re", path), path + ".re[]").ToImmutableArray() : ImmutableArray<Reaction>.Empty,
			};
		}).ToArray();
	}

	[SuppressMessage("ReSharper", "ConvertToLambdaExpression")]
	private static IEnumerable<Attachment> ReadMessageAttachments(JsonElement.ArrayEnumerator attachmentsArray, FakeSnowflake fakeSnowflake, string path) {
		return attachmentsArray.Select(attachmentObj => {
			string url = attachmentObj.RequireString("url", path);
			string name = url[(url.LastIndexOf('/') + 1)..];
			string? type = ContentTypeProvider.TryGetContentType(name, out var contentType) ? contentType : null;

			return new Attachment {
				Id = fakeSnowflake.Next(),
				Name = name,
				Type = type,
				Url = url,
				Size = 0, // unknown size
			};
		}).DistinctByKeyStable(static attachment => {
			// Some Discord messages have duplicate attachments with the same id for unknown reasons.
			return attachment.Id;
		});
	}

	private static IEnumerable<Embed> ReadMessageEmbeds(JsonElement.ArrayEnumerator embedsArray, string path) {
		// Some rich embeds are missing URLs which causes a missing 'url' key.
		return embedsArray.Where(static embedObj => embedObj.HasKey("url")).Select(embedObj => {
			string url = embedObj.RequireString("url", path);
			string type = embedObj.RequireString("type", path);

			var embedJson = new Dictionary<string, object> {
				{ "url", url },
				{ "type", type },
				{ "dht_legacy", true },
			};

			if (type == "image") {
				embedJson["image"] = new Dictionary<string, string> {
					{ "url", url }
				};
			}
			else if (type == "rich") {
				if (embedObj.HasKey("t")) {
					embedJson["title"] = embedObj.RequireString("t", path);
				}

				if (embedObj.HasKey("d")) {
					embedJson["description"] = embedObj.RequireString("d", path);
				}
			}

			return new Embed {
				Json = JsonSerializer.Serialize(embedJson)
			};
		});
	}

	private static IEnumerable<Reaction> ReadMessageReactions(JsonElement.ArrayEnumerator reactionsArray, string path) {
		return reactionsArray.Select(reactionObj => {
			var id = reactionObj.HasKey("id") ? reactionObj.RequireSnowflake("id", path) : (ulong?) null;
			var name = reactionObj.HasKey("n") ? reactionObj.RequireString("n", path) : null;

			if (id == null && name == null) {
				throw new JsonException("Expected key '" + path + ".id' and/or '" + path + ".n' to be present.");
			}

			return new Reaction {
				EmojiId = id,
				EmojiName = name,
				EmojiFlags = reactionObj.HasKey("an") && reactionObj.RequireBool("an", path) ? EmojiFlags.Animated : EmojiFlags.None,
				Count = reactionObj.RequireInt("c", path, min: 0),
			};
		});
	}
}
