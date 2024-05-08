using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Utils.Logging;

namespace DHT.Server.Database.Export;

static class ViewerJsonExport {
	private static readonly Log Log = Log.ForType(typeof(ViewerJsonExport));

	public static async Task GetMetadata(Stream stream, IDatabaseFile db, MessageFilter? filter = null, CancellationToken cancellationToken = default) {
		var perf = Log.Start();

		var includedChannels = new List<Channel>();
		var includedServerIds = new HashSet<ulong>();

		var channelIdFilter = filter?.ChannelIds;

		await foreach (var channel in db.Channels.Get(cancellationToken)) {
			if (channelIdFilter == null || channelIdFilter.Contains(channel.Id)) {
				includedChannels.Add(channel);
				includedServerIds.Add(channel.Server);
			}
		}

		var users = await GenerateUserList(db, cancellationToken);
		var servers = await GenerateServerList(db, includedServerIds, cancellationToken);
		var channels = GenerateChannelList(includedChannels);

		var meta = new ViewerJson.JsonMeta {
			Users = users,
			Servers = servers,
			Channels = channels
		};

		perf.Step("Collect database data");

		await JsonSerializer.SerializeAsync(stream, meta, ViewerJsonMetadataContext.Default.JsonMeta, cancellationToken);

		perf.Step("Serialize to JSON");
		perf.End();
	}

	public static async Task GetMessages(Stream stream, IDatabaseFile db, MessageFilter? filter = null, CancellationToken cancellationToken = default) {
		var perf = Log.Start();

		ReadOnlyMemory<byte> newLine = "\n"u8.ToArray();
		
		await foreach(var message in GenerateMessageList(db, filter, cancellationToken)) {
			await JsonSerializer.SerializeAsync(stream, message, ViewerJsonMessageContext.Default.JsonMessage, cancellationToken);
			await stream.WriteAsync(newLine, cancellationToken);
		}

		perf.Step("Generate and serialize messages to JSON");
		perf.End();
	}

	private static async Task<Dictionary<Snowflake, ViewerJson.JsonUser>> GenerateUserList(IDatabaseFile db, CancellationToken cancellationToken) {
		var users = new Dictionary<Snowflake, ViewerJson.JsonUser>();

		await foreach (var user in db.Users.Get(cancellationToken)) {
			users[user.Id] = new ViewerJson.JsonUser {
				Name = user.Name,
				DisplayName = user.DisplayName,
				Avatar = user.AvatarUrl,
				Tag = user.Discriminator
			};
		}

		return users;
	}

	private static async Task<Dictionary<Snowflake, ViewerJson.JsonServer>> GenerateServerList(IDatabaseFile db, HashSet<ulong> serverIds, CancellationToken cancellationToken) {
		var servers = new Dictionary<Snowflake, ViewerJson.JsonServer>();

		await foreach (var server in db.Servers.Get(cancellationToken)) {
			if (!serverIds.Contains(server.Id)) {
				continue;
			}

			servers[server.Id] = new ViewerJson.JsonServer {
				Name = server.Name,
				Type = ServerTypes.ToJsonViewerString(server.Type)
			};
		}

		return servers;
	}

	private static Dictionary<Snowflake, ViewerJson.JsonChannel> GenerateChannelList(List<Channel> includedChannels) {
		var channels = new Dictionary<Snowflake, ViewerJson.JsonChannel>();

		foreach (var channel in includedChannels) {
			channels[channel.Id] = new ViewerJson.JsonChannel {
				Server = channel.Server,
				Name = channel.Name,
				Parent = channel.ParentId?.ToString(),
				Position = channel.Position,
				Topic = channel.Topic,
				Nsfw = channel.Nsfw
			};
		}

		return channels;
	}

	private static async IAsyncEnumerable<ViewerJson.JsonMessage> GenerateMessageList(IDatabaseFile db, MessageFilter? filter, [EnumeratorCancellation] CancellationToken cancellationToken) {
		await foreach (var message in db.Messages.Get(filter, cancellationToken)) {
			yield return new ViewerJson.JsonMessage {
				Id = message.Id,
				C = message.Channel,
				U = message.Sender,
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
	}
}
