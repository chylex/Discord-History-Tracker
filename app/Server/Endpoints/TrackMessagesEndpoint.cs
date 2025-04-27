using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Server.Download;
using DHT.Utils.Collections;
using DHT.Utils.Http;
using Sisk.Core.Http;

namespace DHT.Server.Endpoints;

sealed class TrackMessagesEndpoint(IDatabaseFile db) : BaseEndpoint {
	private const string HasNewMessages = "1";
	private const string NoNewMessages = "0";
	
	protected override async Task<HttpResponse> Respond(HttpRequest request) {
		JsonElement root = await ReadJson(request);
		
		if (root.ValueKind != JsonValueKind.Array) {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected root element to be an array.");
		}
		
		var addedMessageIds = new HashSet<ulong>();
		var messages = new Message[root.GetArrayLength()];
		
		int i = 0;
		foreach (JsonElement ele in root.EnumerateArray()) {
			Message message = ReadMessage(ele, "message");
			messages[i++] = message;
			addedMessageIds.Add(message.Id);
		}
		
		var addedMessageFilter = new MessageFilter { MessageIds = addedMessageIds };
		bool anyNewMessages = await db.Messages.Count(addedMessageFilter, CancellationToken.None) < addedMessageIds.Count;
		
		await db.Messages.Add(messages);
		
		return new HttpResponse().WithContent(anyNewMessages ? HasNewMessages : NoNewMessages);
	}
	
	private static Message ReadMessage(JsonElement json, string path) {
		return new Message  {
			Id = json.RequireSnowflake("id", path),
			Sender = json.RequireSnowflake("sender", path),
			Channel = json.RequireSnowflake("channel", path),
			Text = json.RequireString("text", path),
			Timestamp = json.RequireLong("timestamp", path),
			EditTimestamp = json.HasKey("editTimestamp") ? json.RequireLong("editTimestamp", path) : null,
			RepliedToId = json.HasKey("repliedToId") ? json.RequireSnowflake("repliedToId", path) : null,
			Attachments = json.HasKey("attachments") ? ReadAttachments(json.RequireArray("attachments", path + ".attachments"), path + ".attachments[]").ToImmutableList() : ImmutableList<Attachment>.Empty,
			Embeds = json.HasKey("embeds") ? ReadEmbeds(json.RequireArray("embeds", path + ".embeds"), path + ".embeds[]").ToImmutableList() : ImmutableList<Embed>.Empty,
			Reactions = json.HasKey("reactions") ? ReadReactions(json.RequireArray("reactions", path + ".reactions"), path + ".reactions[]").ToImmutableList() : ImmutableList<Reaction>.Empty,
		};
	}
	
	[SuppressMessage("ReSharper", "ConvertToLambdaExpression")]
	private static IEnumerable<Attachment> ReadAttachments(JsonElement.ArrayEnumerator array, string path) {
		return array.Select(ele => {
			string downloadUrl = ele.RequireString("url", path);
			return new Attachment {
				Id = ele.RequireSnowflake("id", path),
				Name = ele.RequireString("name", path),
				Type = ele.HasKey("type") ? ele.RequireString("type", path) : null,
				NormalizedUrl = DiscordCdn.NormalizeUrl(downloadUrl),
				DownloadUrl = downloadUrl,
				Size = (ulong) ele.RequireLong("size", path),
				Width = ele.HasKey("width") ? ele.RequireInt("width", path) : null,
				Height = ele.HasKey("height") ? ele.RequireInt("height", path) : null,
			};
		}).DistinctByKeyStable(static attachment => {
			// Some Discord messages have duplicate attachments with the same id for unknown reasons.
			return attachment.Id;
		});
	}
	
	private static IEnumerable<Embed> ReadEmbeds(JsonElement.ArrayEnumerator array, string path) {
		return array.Select(ele => new Embed {
			Json = ele.ValueKind == JsonValueKind.String ? ele.ToString() : throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + "' to be a string."),
		});
	}
	
	private static IEnumerable<Reaction> ReadReactions(JsonElement.ArrayEnumerator array, string path) {
		return array.Select(ele => {
			var reaction = new Reaction {
				EmojiId = ele.HasKey("id") ? ele.RequireSnowflake("id", path) : null,
				EmojiName = ele.HasKey("name") ? ele.RequireString("name", path) : null,
				EmojiFlags = ReadEmojiFlag(ele, "isAnimated", path, EmojiFlags.Animated),
				Count = ele.RequireInt("count", path),
			};
			
			if (reaction.EmojiId == null && reaction.EmojiName == null) {
				throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + ".id' and/or '" + path + ".name' to be present.");
			}
			
			return reaction;
		});
	}
	
	private static EmojiFlags ReadEmojiFlag(JsonElement ele, string key, string path, EmojiFlags flag) {
		return ele.HasKey(key) && ele.RequireBool(key, path) ? flag : EmojiFlags.None;
	}
}
