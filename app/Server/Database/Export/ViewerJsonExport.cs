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

		var value = new ViewerJson {
			Meta = new ViewerJson.JsonMeta {
				Users = users,
				Userindex = userindex,
				Servers = servers,
				Channels = channels
			},
			Data = GenerateMessageList(includedMessages, userIndices, strategy)
		};

		perf.Step("Generate value object");

		await JsonSerializer.SerializeAsync(stream, value, ViewerJsonContext.Default.ViewerJson);

		perf.Step("Serialize to JSON");
		perf.End();
	}

	private static Dictionary<Snowflake, ViewerJson.JsonUser> GenerateUserList(IDatabaseFile db, HashSet<ulong> userIds, out List<Snowflake> userindex, out Dictionary<ulong, int> userIndices) {
		var users = new Dictionary<Snowflake, ViewerJson.JsonUser>();
		userindex = new List<Snowflake>();
		userIndices = new Dictionary<ulong, int>();

		foreach (var user in db.GetAllUsers()) {
			var id = user.Id;
			if (!userIds.Contains(id)) {
				continue;
			}

			var idSnowflake = new Snowflake(id);
			userIndices[id] = users.Count;
			userindex.Add(idSnowflake);
			
			users[idSnowflake] = new ViewerJson.JsonUser {
				Name = user.Name,
				Avatar = user.AvatarUrl,
				Tag = user.Discriminator
			};
		}

		return users;
	}

	private static List<ViewerJson.JsonServer> GenerateServerList(IDatabaseFile db, HashSet<ulong> serverIds, out Dictionary<ulong, int> serverIndices) {
		var servers = new List<ViewerJson.JsonServer>();
		serverIndices = new Dictionary<ulong, int>();

		foreach (var server in db.GetAllServers()) {
			var id = server.Id;
			if (!serverIds.Contains(id)) {
				continue;
			}

			serverIndices[id] = servers.Count;
			
			servers.Add(new ViewerJson.JsonServer {
				Name = server.Name,
				Type = ServerTypes.ToJsonViewerString(server.Type)
			});
		}

		return servers;
	}

	private static Dictionary<Snowflake, ViewerJson.JsonChannel> GenerateChannelList(List<Channel> includedChannels, Dictionary<ulong, int> serverIndices) {
		var channels = new Dictionary<Snowflake, ViewerJson.JsonChannel>();

		foreach (var channel in includedChannels) {
			var channelIdSnowflake = new Snowflake(channel.Id);
			
			channels[channelIdSnowflake] = new ViewerJson.JsonChannel {
				Server = serverIndices[channel.Server],
				Name = channel.Name,
				Parent = channel.ParentId?.ToString(),
				Position = channel.Position,
				Topic = channel.Topic,
				Nsfw = channel.Nsfw
			};
		}

		return channels;
	}

	private static Dictionary<Snowflake, Dictionary<Snowflake, ViewerJson.JsonMessage>> GenerateMessageList(List<Message> includedMessages, Dictionary<ulong, int> userIndices, IViewerExportStrategy strategy) {
		var data = new Dictionary<Snowflake, Dictionary<Snowflake, ViewerJson.JsonMessage>>();

		foreach (var grouping in includedMessages.GroupBy(static message => message.Channel)) {
			var channelIdSnowflake = new Snowflake(grouping.Key);
			var channelData = new Dictionary<Snowflake, ViewerJson.JsonMessage>();

			foreach (var message in grouping) {
				var messageIdSnowflake = new Snowflake(message.Id);
				
				channelData[messageIdSnowflake] = new ViewerJson.JsonMessage {
					U = userIndices[message.Sender],
					T = message.Timestamp,
					M = string.IsNullOrEmpty(message.Text) ? null : message.Text,
					Te = message.EditTimestamp,
					R = message.RepliedToId?.ToString(),
					
					A = message.Attachments.IsEmpty ? null : message.Attachments.Select(attachment => {
						var a = new ViewerJson.JsonMessageAttachment {
							Url = strategy.GetAttachmentUrl(attachment),
							Name = Uri.TryCreate(attachment.NormalizedUrl, UriKind.Absolute, out var uri) ? Path.GetFileName(uri.LocalPath) : attachment.NormalizedUrl
						};

						if (attachment is { Width: not null, Height: not null }) {
							a.Width = attachment.Width;
							a.Height = attachment.Height;
						}

						return a;
					}).ToArray(),
					
					E = message.Embeds.IsEmpty ? null : message.Embeds.Select(static embed => embed.Json).ToArray(),
					
					Re = message.Reactions.IsEmpty ? null : message.Reactions.Select(static reaction => new ViewerJson.JsonMessageReaction {
						Id = reaction.EmojiId?.ToString(),
						N = reaction.EmojiName,
						A = reaction.EmojiFlags.HasFlag(EmojiFlags.Animated),
						C = reaction.Count
					}).ToArray()
				};
			}

			data[channelIdSnowflake] = channelData;
		}

		return data;
	}
}
