using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Export.Strategy;
using DHT.Utils.Logging;

namespace DHT.Server.Database.Export;

public static class ViewerJsonExport {
	private static readonly Log Log = Log.ForType(typeof(ViewerJsonExport));

	public static async Task Generate(Stream stream, IViewerExportStrategy strategy, IDatabaseFile db, MessageFilter? filter = null) {
		var perf = Log.Start();

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

		var users = GenerateUserList(db, includedUserIds, out var userindex, out var userIndices);
		var servers = GenerateServerList(db, includedServerIds, out var serverindex);
		var channels = GenerateChannelList(includedChannels, serverindex);

		perf.Step("Collect database data");

		var value = new {
			meta = new { users, userindex, servers, channels },
			data = GenerateMessageList(includedMessages, userIndices, strategy),
		};

		perf.Step("Generate value object");

		var opts = new JsonSerializerOptions();
		opts.Converters.Add(new ViewerJsonSnowflakeSerializer());

		await JsonSerializer.SerializeAsync(stream, value, opts);

		perf.Step("Serialize to JSON");
		perf.End();
	}

	private static Dictionary<string, object> GenerateUserList(IDatabaseFile db, HashSet<ulong> userIds, out List<string> userindex, out Dictionary<ulong, object> userIndices) {
		var users = new Dictionary<string, object>();
		userindex = new List<string>();
		userIndices = new Dictionary<ulong, object>();

		foreach (var user in db.GetAllUsers()) {
			var id = user.Id;
			if (!userIds.Contains(id)) {
				continue;
			}

			var obj = new Dictionary<string, object> {
				["name"] = user.Name
			};

			if (user.AvatarUrl != null) {
				obj["avatar"] = user.AvatarUrl;
			}

			if (user.Discriminator != null) {
				obj["tag"] = user.Discriminator;
			}

			var idStr = id.ToString();
			userIndices[id] = users.Count;
			userindex.Add(idStr);
			users[idStr] = obj;
		}

		return users;
	}

	private static List<object> GenerateServerList(IDatabaseFile db, HashSet<ulong> serverIds, out Dictionary<ulong, object> serverIndices) {
		var servers = new List<object>();
		serverIndices = new Dictionary<ulong, object>();

		foreach (var server in db.GetAllServers()) {
			var id = server.Id;
			if (!serverIds.Contains(id)) {
				continue;
			}

			serverIndices[id] = servers.Count;
			servers.Add(new Dictionary<string, object> {
				["name"] = server.Name,
				["type"] = ServerTypes.ToJsonViewerString(server.Type),
			});
		}

		return servers;
	}

	private static Dictionary<string, object> GenerateChannelList(List<Channel> includedChannels, Dictionary<ulong, object> serverIndices) {
		var channels = new Dictionary<string, object>();

		foreach (var channel in includedChannels) {
			var obj = new Dictionary<string, object> {
				["server"] = serverIndices[channel.Server],
				["name"] = channel.Name,
			};

			if (channel.ParentId != null) {
				obj["parent"] = channel.ParentId;
			}

			if (channel.Position != null) {
				obj["position"] = channel.Position;
			}

			if (channel.Topic != null) {
				obj["topic"] = channel.Topic;
			}

			if (channel.Nsfw != null) {
				obj["nsfw"] = channel.Nsfw;
			}

			channels[channel.Id.ToString()] = obj;
		}

		return channels;
	}

	private static Dictionary<string, Dictionary<string, object>> GenerateMessageList( List<Message> includedMessages, Dictionary<ulong, object> userIndices, IViewerExportStrategy strategy) {
		var data = new Dictionary<string, Dictionary<string, object>>();

		foreach (var grouping in includedMessages.GroupBy(static message => message.Channel)) {
			var channel = grouping.Key.ToString();
			var channelData = new Dictionary<string, object>();

			foreach (var message in grouping) {
				var obj = new Dictionary<string, object> {
					["u"] = userIndices[message.Sender],
					["t"] = message.Timestamp,
				};

				if (!string.IsNullOrEmpty(message.Text)) {
					obj["m"] = message.Text;
				}

				if (message.EditTimestamp != null) {
					obj["te"] = message.EditTimestamp;
				}

				if (message.RepliedToId != null) {
					obj["r"] = message.RepliedToId.Value;
				}

				if (!message.Attachments.IsEmpty) {
					obj["a"] = message.Attachments.Select(attachment => {
						var a = new Dictionary<string, object> {
							{ "url", strategy.GetAttachmentUrl(attachment) },
							{ "name", Uri.TryCreate(attachment.Url, UriKind.Absolute, out var uri) ? Path.GetFileName(uri.LocalPath) : attachment.Url },
						};

						if (attachment is { Width: not null, Height: not null }) {
							a["width"] = attachment.Width;
							a["height"] = attachment.Height;
						}

						return a;
					}).ToArray();
				}

				if (!message.Embeds.IsEmpty) {
					obj["e"] = message.Embeds.Select(static embed => embed.Json).ToArray();
				}

				if (!message.Reactions.IsEmpty) {
					obj["re"] = message.Reactions.Select(static reaction => {
						var r = new Dictionary<string, object>();

						if (reaction.EmojiId != null) {
							r["id"] = reaction.EmojiId.Value;
						}

						if (reaction.EmojiName != null) {
							r["n"] = reaction.EmojiName;
						}

						r["a"] = reaction.EmojiFlags.HasFlag(EmojiFlags.Animated);
						r["c"] = reaction.Count;
						return r;
					}).ToArray();
				}

				channelData[message.Id.ToString()] = obj;
			}

			data[channel] = channelData;
		}

		return data;
	}
}
