using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Utils.Logging;
using Channel = System.Threading.Channels.Channel;
using DiscordChannel = DHT.Server.Data.Channel;

namespace DHT.Server.Database.Export;

static class ViewerJsonExport {
	private static readonly Log Log = Log.ForType(typeof(ViewerJsonExport));
	
	public static async Task GetMetadata(Stream stream, IDatabaseFile db, MessageFilter? filter = null, CancellationToken cancellationToken = default) {
		Perf perf = Log.Start();
		
		var includedChannels = new List<DiscordChannel>();
		var includedServerIds = new HashSet<ulong>();
		
		HashSet<ulong>? channelIdFilter = filter?.ChannelIds;
		
		await foreach (DiscordChannel channel in db.Channels.Get(cancellationToken)) {
			if (channelIdFilter == null || channelIdFilter.Contains(channel.Id)) {
				includedChannels.Add(channel);
				includedServerIds.Add(channel.Server);
			}
		}
		
		Dictionary<Snowflake, ViewerJson.JsonUser> users = await GenerateUserList(db, cancellationToken);
		Dictionary<Snowflake, ViewerJson.JsonServer> servers = await GenerateServerList(db, includedServerIds, cancellationToken);
		Dictionary<Snowflake, ViewerJson.JsonChannel> channels = GenerateChannelList(includedChannels);
		
		var meta = new ViewerJson.JsonMeta {
			Users = users,
			Servers = servers,
			Channels = channels,
		};
		
		perf.Step("Collect database data");
		
		await JsonSerializer.SerializeAsync(stream, meta, ViewerJsonMetadataContext.Default.JsonMeta, cancellationToken);
		
		perf.Step("Serialize to JSON");
		perf.End();
	}
	
	public static async Task GetMessages(Stream stream, IDatabaseFile db, MessageFilter? filter = null, CancellationToken cancellationToken = default) {
		Perf perf = Log.Start();
		
		ReadOnlyMemory<byte> newLine = "\n"u8.ToArray();
		
		Channel<Message> channel = Channel.CreateBounded<Message>(new BoundedChannelOptions(32) {
			SingleWriter = true,
			SingleReader = true,
			AllowSynchronousContinuations = true,
			FullMode = BoundedChannelFullMode.Wait,
		});
		
		Task writerTask = Task.Run(async () => {
			try {
				await foreach (Message message in db.Messages.Get(filter, cancellationToken)) {
					await channel.Writer.WriteAsync(message, cancellationToken);
				}
			} finally {
				channel.Writer.Complete();
			}
		}, cancellationToken);
		
		await foreach (Message message in channel.Reader.ReadAllAsync(cancellationToken)) {
			await JsonSerializer.SerializeAsync(stream, ToJsonMessage(message), ViewerJsonMessageContext.Default.JsonMessage, cancellationToken);
			await stream.WriteAsync(newLine, cancellationToken);
		}
		
		await writerTask;
		
		perf.Step("Generate and serialize messages to JSON");
		perf.End();
	}
	
	private static async Task<Dictionary<Snowflake, ViewerJson.JsonUser>> GenerateUserList(IDatabaseFile db, CancellationToken cancellationToken) {
		var users = new Dictionary<Snowflake, ViewerJson.JsonUser>();
		
		await foreach (User user in db.Users.Get(cancellationToken)) {
			users[user.Id] = new ViewerJson.JsonUser {
				Name = user.Name,
				DisplayName = user.DisplayName,
				Avatar = user.AvatarHash,
			};
		}
		
		return users;
	}
	
	private static async Task<Dictionary<Snowflake, ViewerJson.JsonServer>> GenerateServerList(IDatabaseFile db, HashSet<ulong> serverIds, CancellationToken cancellationToken) {
		var servers = new Dictionary<Snowflake, ViewerJson.JsonServer>();
		
		await foreach (Data.Server server in db.Servers.Get(cancellationToken)) {
			if (!serverIds.Contains(server.Id)) {
				continue;
			}
			
			servers[server.Id] = new ViewerJson.JsonServer {
				Name = server.Name,
				Type = ServerTypes.ToJsonViewerString(server.Type),
				IconUrl = server.IconUrl?.DownloadUrl,
			};
		}
		
		return servers;
	}
	
	private static Dictionary<Snowflake, ViewerJson.JsonChannel> GenerateChannelList(List<DiscordChannel> includedChannels) {
		var channels = new Dictionary<Snowflake, ViewerJson.JsonChannel>();
		
		foreach (DiscordChannel channel in includedChannels) {
			channels[channel.Id] = new ViewerJson.JsonChannel {
				Server = channel.Server,
				Name = channel.Name,
				Parent = channel.ParentId,
				Position = channel.Position,
				Topic = channel.Topic,
				Nsfw = channel.Nsfw,
			};
		}
		
		return channels;
	}
	
	private static ViewerJson.JsonMessage ToJsonMessage(Message message) {
		return new ViewerJson.JsonMessage {
			Id = message.Id,
			C = message.Channel,
			U = message.Sender,
			T = message.Timestamp,
			M = string.IsNullOrEmpty(message.Text) ? null : message.Text,
			Te = message.EditTimestamp,
			R = message.RepliedToId,
			
			A = message.Attachments.IsEmpty ? null : message.Attachments.Select(static attachment => {
				var a = new ViewerJson.JsonMessageAttachment {
					Url = attachment.DownloadUrl,
					Name = Uri.TryCreate(attachment.NormalizedUrl, UriKind.Absolute, out Uri? uri) ? Path.GetFileName(uri.LocalPath) : attachment.NormalizedUrl,
				};
				
				if (attachment is { Width: not null, Height: not null }) {
					a.Width = attachment.Width;
					a.Height = attachment.Height;
				}
				
				return a;
			}).ToArray(),
			
			E = message.Embeds.IsEmpty ? null : message.Embeds.Select(static embed => embed.Json).ToArray(),
			
			Re = message.Reactions.IsEmpty ? null : message.Reactions.Select(static reaction => new ViewerJson.JsonMessageReaction {
				Id = reaction.EmojiId,
				N = reaction.EmojiName,
				A = reaction.EmojiFlags.HasFlag(EmojiFlags.Animated),
				C = reaction.Count,
			}).ToArray(),
		};
	}
}
