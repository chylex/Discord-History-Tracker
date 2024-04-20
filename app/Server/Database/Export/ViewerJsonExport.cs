using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Utils.Logging;

namespace DHT.Server.Database.Export;

static class ViewerJsonExport {
	private static readonly Log Log = Log.ForType(typeof(ViewerJsonExport));

	public static async Task Generate(Stream stream, IDatabaseFile db, MessageFilter? filter = null, CancellationToken cancellationToken = default) {
		var perf = Log.Start();

		var includedUserIds = new HashSet<ulong>();
		var includedChannelIds = new HashSet<ulong>();
		var includedServerIds = new HashSet<ulong>();

		var includedMessages = await db.Messages.Get(filter, cancellationToken).ToListAsync(cancellationToken);
		var includedChannels = new List<Channel>();

		foreach (var message in includedMessages) {
			includedUserIds.Add(message.Sender);
			includedChannelIds.Add(message.Channel);
		}

		await foreach (var channel in db.Channels.Get(cancellationToken)) {
			if (includedChannelIds.Contains(channel.Id)) {
				includedChannels.Add(channel);
				includedServerIds.Add(channel.Server);
			}
		}

		var (users, userIndex, userIndices) = await GenerateUserList(db, includedUserIds, cancellationToken);
		var (servers, serverIndices) = await GenerateServerList(db, includedServerIds, cancellationToken);
		var channels = GenerateChannelList(includedChannels, serverIndices);

		perf.Step("Collect database data");

		var value = new ViewerJson {
			Meta = new ViewerJson.JsonMeta {
				Users = users,
				Userindex = userIndex,
				Servers = servers,
				Channels = channels
			},
			Data = GenerateMessageList(includedMessages, userIndices)
		};

		perf.Step("Generate value object");

		await JsonSerializer.SerializeAsync(stream, value, ViewerJsonContext.Default.ViewerJson, cancellationToken);

		perf.Step("Serialize to JSON");
		perf.End();
	}

	private static async Task<(Dictionary<Snowflake, ViewerJson.JsonUser> Users, List<Snowflake> UserIndex, Dictionary<ulong, int> UserIndices)> GenerateUserList(IDatabaseFile db, HashSet<ulong> userIds, CancellationToken cancellationToken) {
		var users = new Dictionary<Snowflake, ViewerJson.JsonUser>();
		var userIndex = new List<Snowflake>();
		var userIndices = new Dictionary<ulong, int>();

		await foreach (var user in db.Users.Get(cancellationToken)) {
			var id = user.Id;
			if (!userIds.Contains(id)) {
				continue;
			}

			var idSnowflake = new Snowflake(id);
			userIndices[id] = users.Count;
			userIndex.Add(idSnowflake);
			
			users[idSnowflake] = new ViewerJson.JsonUser {
				Name = user.Name,
				Avatar = user.AvatarUrl,
				Tag = user.Discriminator
			};
		}

		return (users, userIndex, userIndices);
	}

	private static async Task<(List<ViewerJson.JsonServer> Servers, Dictionary<ulong, int> ServerIndices)> GenerateServerList(IDatabaseFile db, HashSet<ulong> serverIds, CancellationToken cancellationToken) {
		var servers = new List<ViewerJson.JsonServer>();
		var serverIndices = new Dictionary<ulong, int>();

		await foreach (var server in db.Servers.Get(cancellationToken)) {
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

		return (servers, serverIndices);
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

	private static Dictionary<Snowflake, Dictionary<Snowflake, ViewerJson.JsonMessage>> GenerateMessageList(List<Message> includedMessages, Dictionary<ulong, int> userIndices) {
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
					
					A = message.Attachments.IsEmpty ? null : message.Attachments.Select(static attachment => {
						var a = new ViewerJson.JsonMessageAttachment {
							Url = attachment.DownloadUrl,
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
