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
		Perf perf = Log.Start();
		JsonElement root = await JsonSerializer.DeserializeAsync(stream, JsonElementContext.Default.JsonElement);
		
		try {
			JsonElement meta = root.RequireObject("meta");
			JsonElement data = root.RequireObject("data");
			
			perf.Step("Deserialize JSON");
			
			User[] users = ReadUserList(meta);
			Data.Server[] servers = ReadServerList(meta, fakeSnowflake);
			
			var newServersOnly = new HashSet<Data.Server>(servers);
			Dictionary<ulong, Data.Server> oldServersById = await db.Servers.Get().ToDictionaryAsync(static server => server.Id, static server => server);
			Dictionary<ulong, Channel> oldChannelsById = await db.Channels.Get().ToDictionaryAsync(static channel => channel.Id, static channel => channel);
			
			foreach ((ulong channelId, int serverIndex) in ReadChannelToServerIndexMapping(meta, servers)) {
				if (oldChannelsById.TryGetValue(channelId, out Channel oldChannel) && oldServersById.TryGetValue(oldChannel.Server, out Data.Server oldServer) && newServersOnly.Remove(servers[serverIndex])) {
					servers[serverIndex] = oldServer;
				}
			}
			
			perf.Step("Read server and user list");
			
			if (newServersOnly.Count > 0) {
				Dictionary<Data.Server, ulong>? askedServerIds = await askForServerIds(newServersOnly.ToArray());
				if (askedServerIds == null) {
					return false;
				}
				
				perf.Step("Ask for server IDs");
				
				for (int i = 0; i < servers.Length; i++) {
					Data.Server server = servers[i];
					if (askedServerIds.TryGetValue(server, out ulong serverId)) {
						servers[i] = server with { Id = serverId };
					}
				}
			}
			
			Channel[] channels = ReadChannelList(meta, servers);
			
			perf.Step("Read channel list");
			
			HashSet<ulong> oldMessageIds = await db.Messages.GetIds().ToHashSetAsync();
			Message[] newMessages = channels.SelectMany(channel => ReadMessages(data, channel, users, fakeSnowflake))
			                                .Where(message => !oldMessageIds.Contains(message.Id))
			                                .ToArray();
			
			perf.Step("Read messages");
			
			await db.Users.Add(users);
			await db.Servers.Add(servers);
			await db.Channels.Add(channels);
			await db.Messages.Add(newMessages);
			
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
		
		Dictionary<ulong, int> userindex = meta.RequireArray("userindex", "meta")
		                                       .Select(static (item, index) => (ParseUserIndex(item, index), index))
		                                       .ToDictionary();
		
		var users = new User[userindex.Count];
		
		foreach (JsonProperty item in meta.RequireObject("users", "meta").EnumerateObject()) {
			string path = UsersPath + "." + item.Name;
			ulong userId = ulong.Parse(item.Name);
			JsonElement userObj = item.Value;
			
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
			string path = ChannelsPath + "." + item.Name;
			ulong channelId = ulong.Parse(item.Name);
			JsonElement channelObj = item.Value;
			
			return (channelId, channelObj.RequireInt("server", path, min: 0, max: servers.Length - 1));
		}).ToDictionary();
	}
	
	private static Channel[] ReadChannelList(JsonElement meta, Data.Server[] servers) {
		return meta.RequireObject("channels", "meta").EnumerateObject().Select(item => {
			string path = ChannelsPath + "." + item.Name;
			ulong channelId = ulong.Parse(item.Name);
			JsonElement channelObj = item.Value;
			
			return new Channel {
				Id = channelId,
				Server = servers[channelObj.RequireInt("server", path, min: 0, servers.Length - 1)].Id,
				Name = channelObj.RequireString("name", path),
				Position = channelObj.HasKey("position") ? channelObj.RequireInt("position", path, min: 0) : null,
				Topic = channelObj.HasKey("topic") ? channelObj.RequireString("topic", path) : null,
				Nsfw = channelObj.HasKey("nsfw") ? channelObj.RequireBool("nsfw", path) : null,
			};
		}).ToArray();
	}
	
	private static Message[] ReadMessages(JsonElement data, Channel channel, User[] users, FakeSnowflake fakeSnowflake) {
		const string DataPath = "data";
		
		ulong channelId = channel.Id;
		string channelIdStr = channelId.ToString();
		
		JsonElement? messagesObj = data.HasKey(channelIdStr) ? data.RequireObject(channelIdStr, DataPath) : null;
		if (messagesObj == null) {
			return [];
		}
		
		return messagesObj.Value.EnumerateObject().Select(item => {
			string path = DataPath + "." + item.Name;
			ulong messageId = ulong.Parse(item.Name);
			JsonElement messageObj = item.Value;
			
			return new Message {
				Id = messageId,
				Sender = users[messageObj.RequireInt("u", path, min: 0, max: users.Length - 1)].Id,
				Channel = channelId,
				Text = messageObj.HasKey("m") ? messageObj.RequireString("m", path) : string.Empty,
				Timestamp = messageObj.RequireLong("t", path),
				EditTimestamp = messageObj.HasKey("te") ? messageObj.RequireLong("te", path) : null,
				RepliedToId = messageObj.HasKey("r") ? messageObj.RequireSnowflake("r", path) : null,
				Attachments = messageObj.HasKey("a") ? ReadMessageAttachments(messageObj.RequireArray("a", path), fakeSnowflake, path + ".a[]").ToImmutableList() : ImmutableList<Attachment>.Empty,
				Embeds = messageObj.HasKey("e") ? ReadMessageEmbeds(messageObj.RequireArray("e", path), path + ".e[]").ToImmutableList() : ImmutableList<Embed>.Empty,
				Reactions = messageObj.HasKey("re") ? ReadMessageReactions(messageObj.RequireArray("re", path), path + ".re[]").ToImmutableList() : ImmutableList<Reaction>.Empty,
			};
		}).ToArray();
	}
	
	[SuppressMessage("ReSharper", "ConvertToLambdaExpression")]
	private static IEnumerable<Attachment> ReadMessageAttachments(JsonElement.ArrayEnumerator attachmentsArray, FakeSnowflake fakeSnowflake, string path) {
		return attachmentsArray.Select(attachmentObj => {
			string url = attachmentObj.RequireString("url", path);
			string name = url[(url.LastIndexOf('/') + 1)..];
			string? type = ContentTypeProvider.TryGetContentType(name, out string? contentType) ? contentType : null;
			
			return new Attachment {
				Id = fakeSnowflake.Next(),
				Name = name,
				Type = type,
				NormalizedUrl = url,
				DownloadUrl = url,
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
			
			var embed = new DiscordEmbedLegacyJson {
				Url = url,
				Type = type,
				Title = type == "rich" && embedObj.HasKey("t") ? embedObj.RequireString("t", path) : null,
				Description = type == "rich" && embedObj.HasKey("d") ? embedObj.RequireString("d", path) : null,
				Image = type == "image" ? new DiscordEmbedLegacyJson.ImageJson { Url = url } : null,
			};
			
			return new Embed {
				Json = JsonSerializer.Serialize(embed, DiscordEmbedLegacyJsonContext.Default.DiscordEmbedLegacyJson),
			};
		});
	}
	
	private static IEnumerable<Reaction> ReadMessageReactions(JsonElement.ArrayEnumerator reactionsArray, string path) {
		return reactionsArray.Select(reactionObj => {
			ulong? id = reactionObj.HasKey("id") ? reactionObj.RequireSnowflake("id", path) : (ulong?) null;
			string? name = reactionObj.HasKey("n") ? reactionObj.RequireString("n", path) : null;
			
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
