using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using DHT.Server.Data;
using DHT.Server.Data.Filters;

namespace DHT.Server.Database.Export {
	public static class ViewerJsonExport {
		public static string Generate(IDatabaseFile db, MessageFilter? filter = null) {
			var includedUserIds = new HashSet<ulong>();
			var includedChannelIds = new HashSet<ulong>();
			var includedServerIds = new HashSet<ulong>();

			var includedMessages = db.GetMessages(filter);
			var includedChannels = new List<Channel>();

			foreach (var message in includedMessages) {
				includedUserIds.Add(message.Sender);
				includedChannelIds.Add(message.Channel);
			}

			foreach (var channel in db.GetAllChannels()) {
				if (includedChannelIds.Contains(channel.Id)) {
					includedChannels.Add(channel);
					includedServerIds.Add(channel.Server);
				}
			}

			var opts = new JsonSerializerOptions();
			opts.Converters.Add(new ViewerJsonSnowflakeSerializer());

			var users = GenerateUserList(db, includedUserIds, out var userindex, out var userIndices);
			var servers = GenerateServerList(db, includedServerIds, out var serverindex);
			var channels = GenerateChannelList(includedChannels, serverindex);

			return JsonSerializer.Serialize(new {
				meta = new { users, userindex, servers, channels },
				data = GenerateMessageList(includedMessages, userIndices)
			}, opts);
		}

		private static dynamic GenerateUserList(IDatabaseFile db, HashSet<ulong> userIds, out List<string> userindex, out Dictionary<ulong, int> userIndices) {
			var users = new Dictionary<string, dynamic>();
			userindex = new List<string>();
			userIndices = new Dictionary<ulong, int>();

			foreach (var user in db.GetAllUsers()) {
				var id = user.Id;
				if (!userIds.Contains(id)) {
					continue;
				}

				dynamic obj = new ExpandoObject();
				obj.name = user.Name;

				if (user.AvatarUrl != null) {
					obj.avatar = user.AvatarUrl;
				}

				if (user.Discriminator != null) {
					obj.tag = user.Discriminator;
				}

				var idStr = id.ToString();
				userIndices[id] = users.Count;
				userindex.Add(idStr);
				users[idStr] = obj;
			}

			return users;
		}

		private static dynamic GenerateServerList(IDatabaseFile db, HashSet<ulong> serverIds, out Dictionary<ulong, int> serverIndices) {
			var servers = new List<dynamic>();
			serverIndices = new Dictionary<ulong, int>();

			foreach (var server in db.GetAllServers()) {
				var id = server.Id;
				if (!serverIds.Contains(id)) {
					continue;
				}

				serverIndices[id] = servers.Count;
				servers.Add(new {
					name = server.Name,
					type = ServerTypes.ToJsonViewerString(server.Type)
				});
			}

			return servers;
		}

		private static dynamic GenerateChannelList(List<Channel> includedChannels, Dictionary<ulong, int> serverIndices) {
			var channels = new Dictionary<string, dynamic>();

			foreach (var channel in includedChannels) {
				dynamic obj = new ExpandoObject();
				obj.server = serverIndices[channel.Server];
				obj.name = channel.Name;

				if (channel.ParentId != null) {
					obj.parent = channel.ParentId;
				}

				if (channel.Position != null) {
					obj.position = channel.Position;
				}

				if (channel.Topic != null) {
					obj.topic = channel.Topic;
				}

				if (channel.Nsfw != null) {
					obj.nsfw = channel.Nsfw;
				}

				channels[channel.Id.ToString()] = obj;
			}

			return channels;
		}

		private static dynamic GenerateMessageList(List<Message> includedMessages, Dictionary<ulong, int> userIndices) {
			var data = new Dictionary<string, Dictionary<string, dynamic>>();

			foreach (var grouping in includedMessages.GroupBy(static message => message.Channel)) {
				var channel = grouping.Key.ToString();
				var channelData = new Dictionary<string, dynamic>();

				foreach (var message in grouping) {
					dynamic obj = new ExpandoObject();
					obj.u = userIndices[message.Sender];
					obj.t = message.Timestamp;

					if (!string.IsNullOrEmpty(message.Text)) {
						obj.m = message.Text;
					}

					if (message.EditTimestamp != null) {
						obj.te = message.EditTimestamp;
					}

					if (message.RepliedToId != null) {
						obj.r = message.RepliedToId.Value;
					}

					if (!message.Attachments.IsEmpty) {
						obj.a = message.Attachments.Select(static attachment => new {
							url = attachment.Url
						}).ToArray();
					}

					if (!message.Embeds.IsEmpty) {
						obj.e = message.Embeds.Select(static embed => embed.Json).ToArray();
					}

					if (!message.Reactions.IsEmpty) {
						obj.re = message.Reactions.Select(static reaction => {
							dynamic r = new ExpandoObject();

							if (reaction.EmojiId != null) {
								r.id = reaction.EmojiId.Value;
							}

							if (reaction.EmojiName != null) {
								r.n = reaction.EmojiName;
							}

							r.a = reaction.EmojiFlags.HasFlag(EmojiFlags.Animated);
							r.c = reaction.Count;
							return r;
						});
					}

					channelData[message.Id.ToString()] = obj;
				}

				data[channel] = channelData;
			}

			return data;
		}
	}
}
