using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Server.Download;
using DHT.Utils.Logging;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Repositories;

sealed class SqliteMessageRepository(SqliteConnectionPool pool, SqliteDownloadRepository downloads) : BaseSqliteRepository(Log), IMessageRepository {
	private static readonly Log Log = Log.ForType<SqliteMessageRepository>();
	
	// Moved outside the Add method due to language injections not working in local methods.
	private static SqliteCommand DeleteByMessageId(ISqliteConnection conn, [LanguageInjection("sql", Prefix = "SELECT * FROM ")] string tableName) {
		return conn.Delete(tableName, ("message_id", SqliteType.Integer));
	}
	
	public async Task Add(IReadOnlyList<Message> messages) {
		if (messages.Count == 0) {
			return;
		}
		
		static async Task ExecuteDeleteByMessageId(SqliteCommand cmd, object id) {
			cmd.Set(":message_id", id);
			await cmd.ExecuteNonQueryAsync();
		}
		
		await using (var conn = await pool.Take()) {
			await conn.BeginTransactionAsync();
			
			await using var messageCmd = conn.Upsert("messages", [
				("message_id", SqliteType.Integer),
				("sender_id", SqliteType.Integer),
				("channel_id", SqliteType.Integer),
				("text", SqliteType.Text),
				("timestamp", SqliteType.Integer),
			]);
			
			await using var attachmentCmd = conn.Upsert("attachments", [
				("attachment_id", SqliteType.Integer),
				("name", SqliteType.Text),
				("type", SqliteType.Text),
				("normalized_url", SqliteType.Text),
				("download_url", SqliteType.Text),
				("size", SqliteType.Integer),
				("width", SqliteType.Integer),
				("height", SqliteType.Integer),
			]);
			
			await using var deleteMessageEditTimestampCmd = DeleteByMessageId(conn, "message_edit_timestamps");
			await using var deleteMessageRepliedToCmd = DeleteByMessageId(conn, "message_replied_to");
			
			await using var deleteMessageAttachmentsCmd = DeleteByMessageId(conn, "message_attachments");
			await using var deleteMessageEmbedsCmd = DeleteByMessageId(conn, "message_embeds");
			await using var deleteMessageReactionsCmd = DeleteByMessageId(conn, "message_reactions");
			
			await using var messageEditTimestampCmd = conn.Insert("message_edit_timestamps", [
				("message_id", SqliteType.Integer),
				("edit_timestamp", SqliteType.Integer),
			]);
			
			await using var messageRepliedToCmd = conn.Insert("message_replied_to", [
				("message_id", SqliteType.Integer),
				("replied_to_id", SqliteType.Integer),
			]);
			
			await using var messageAttachmentCmd = conn.Insert("message_attachments", [
				("message_id", SqliteType.Integer),
				("attachment_id", SqliteType.Integer),
			]);
			
			await using var messageEmbedCmd = conn.Insert("message_embeds", [
				("message_id", SqliteType.Integer),
				("json", SqliteType.Text),
			]);
			
			await using var messageReactionCmd = conn.Insert("message_reactions", [
				("message_id", SqliteType.Integer),
				("emoji_id", SqliteType.Integer),
				("emoji_name", SqliteType.Text),
				("emoji_flags", SqliteType.Integer),
				("count", SqliteType.Integer),
			]);
			
			await using var downloadCollector = new SqliteDownloadRepository.NewDownloadCollector(downloads, conn);
			
			foreach (Message message in messages) {
				object messageId = message.Id;
				
				messageCmd.Set(":message_id", messageId);
				messageCmd.Set(":sender_id", message.Sender);
				messageCmd.Set(":channel_id", message.Channel);
				messageCmd.Set(":text", message.Text);
				messageCmd.Set(":timestamp", message.Timestamp);
				await messageCmd.ExecuteNonQueryAsync();
				
				await ExecuteDeleteByMessageId(deleteMessageEditTimestampCmd, messageId);
				await ExecuteDeleteByMessageId(deleteMessageRepliedToCmd, messageId);
				
				await ExecuteDeleteByMessageId(deleteMessageAttachmentsCmd, messageId);
				await ExecuteDeleteByMessageId(deleteMessageEmbedsCmd, messageId);
				await ExecuteDeleteByMessageId(deleteMessageReactionsCmd, messageId);
				
				if (message.EditTimestamp is {} timestamp) {
					messageEditTimestampCmd.Set(":message_id", messageId);
					messageEditTimestampCmd.Set(":edit_timestamp", timestamp);
					await messageEditTimestampCmd.ExecuteNonQueryAsync();
				}
				
				if (message.RepliedToId is {} repliedToId) {
					messageRepliedToCmd.Set(":message_id", messageId);
					messageRepliedToCmd.Set(":replied_to_id", repliedToId);
					await messageRepliedToCmd.ExecuteNonQueryAsync();
				}
				
				if (!message.Attachments.IsEmpty) {
					foreach (Attachment attachment in message.Attachments) {
						object attachmentId = attachment.Id;
						
						attachmentCmd.Set(":attachment_id", attachmentId);
						attachmentCmd.Set(":name", attachment.Name);
						attachmentCmd.Set(":type", attachment.Type);
						attachmentCmd.Set(":normalized_url", attachment.NormalizedUrl);
						attachmentCmd.Set(":download_url", attachment.DownloadUrl);
						attachmentCmd.Set(":size", attachment.Size);
						attachmentCmd.Set(":width", attachment.Width);
						attachmentCmd.Set(":height", attachment.Height);
						await attachmentCmd.ExecuteNonQueryAsync();
						
						messageAttachmentCmd.Set(":message_id", messageId);
						messageAttachmentCmd.Set(":attachment_id", attachmentId);
						await messageAttachmentCmd.ExecuteNonQueryAsync();
						
						await downloadCollector.Add(new Data.Download(attachment.NormalizedUrl, attachment.DownloadUrl, DownloadStatus.Pending, attachment.Type, attachment.Size));
					}
				}
				
				if (!message.Embeds.IsEmpty) {
					foreach (Embed embed in message.Embeds) {
						messageEmbedCmd.Set(":message_id", messageId);
						messageEmbedCmd.Set(":json", embed.Json);
						await messageEmbedCmd.ExecuteNonQueryAsync();
						
						if (DownloadLinkExtractor.TryFromEmbedJson(embed.Json) is {} download) {
							await downloadCollector.Add(download.ToPendingDownload());
						}
					}
				}
				
				if (!message.Reactions.IsEmpty) {
					foreach (Reaction reaction in message.Reactions) {
						messageReactionCmd.Set(":message_id", messageId);
						messageReactionCmd.Set(":emoji_id", reaction.EmojiId);
						messageReactionCmd.Set(":emoji_name", reaction.EmojiName);
						messageReactionCmd.Set(":emoji_flags", (int) reaction.EmojiFlags);
						messageReactionCmd.Set(":count", reaction.Count);
						await messageReactionCmd.ExecuteNonQueryAsync();
						
						if (reaction.EmojiId is {} emojiId) {
							await downloadCollector.Add(DownloadLinkExtractor.Emoji(emojiId, reaction.EmojiFlags).ToPendingDownload());
						}
					}
				}
			}
			
			await conn.CommitTransactionAsync();
			downloadCollector.OnCommitted();
		}
		
		UpdateTotalCount();
	}
	
	public override Task<long> Count(CancellationToken cancellationToken) {
		return Count(filter: null, cancellationToken);
	}
	
	public async Task<long> Count(MessageFilter? filter, CancellationToken cancellationToken) {
		await using var conn = await pool.Take();
		return await conn.ExecuteReaderAsync("SELECT COUNT(*) FROM messages" + filter.GenerateConditions().BuildWhereClause(), static reader => reader?.GetInt64(0) ?? 0L, cancellationToken);
	}
	
	private sealed class MessageToManyCommand<T> : IAsyncDisposable {
		private readonly SqliteCommand cmd;
		private readonly Func<SqliteDataReader, T> readItem;
		
		public MessageToManyCommand(ISqliteConnection conn, string sql, Func<SqliteDataReader, T> readItem) {
			this.cmd = conn.Command(sql);
			this.cmd.Add(":message_id", SqliteType.Integer);
			
			this.readItem = readItem;
		}
		
		public async Task<ImmutableList<T>> GetItems(ulong messageId) {
			cmd.Set(":message_id", messageId);
			
			var items = ImmutableList<T>.Empty;
			
			await using var reader = await cmd.ExecuteReaderAsync();
			
			while (await reader.ReadAsync()) {
				items = items.Add(readItem(reader));
			}
			
			return items;
		}
		
		public async ValueTask DisposeAsync() {
			await cmd.DisposeAsync();
		}
	}
	
	public async IAsyncEnumerable<Message> Get(MessageFilter? filter, [EnumeratorCancellation] CancellationToken cancellationToken) {
		await using var conn = await pool.Take();
		
		const string AttachmentSql =
			"""
			SELECT attachment_id, name, type, normalized_url, download_url, size, width, height
			FROM attachments
			JOIN message_attachments USING (attachment_id)
			WHERE message_attachments.message_id = :message_id
			""";
		
		await using var attachmentCmd = new MessageToManyCommand<Attachment>(conn, AttachmentSql, static reader => new Attachment {
			Id = reader.GetUint64(0),
			Name = reader.GetString(1),
			Type = reader.IsDBNull(2) ? null : reader.GetString(2),
			NormalizedUrl = reader.GetString(3),
			DownloadUrl = reader.GetString(4),
			Size = reader.GetUint64(5),
			Width = reader.IsDBNull(6) ? null : reader.GetInt32(6),
			Height = reader.IsDBNull(7) ? null : reader.GetInt32(7),
		});
		
		const string EmbedSql =
			"""
			SELECT json
			FROM message_embeds
			WHERE message_id = :message_id
			""";
		
		await using var embedCmd = new MessageToManyCommand<Embed>(conn, EmbedSql, static reader => new Embed {
			Json = reader.GetString(0),
		});
		
		const string ReactionSql =
			"""
			SELECT emoji_id, emoji_name, emoji_flags, count
			FROM message_reactions
			WHERE message_id = :message_id
			""";
		
		await using var reactionsCmd = new MessageToManyCommand<Reaction>(conn, ReactionSql, static reader => new Reaction {
			EmojiId = reader.IsDBNull(0) ? null : reader.GetUint64(0),
			EmojiName = reader.IsDBNull(1) ? null : reader.GetString(1),
			EmojiFlags = (EmojiFlags) reader.GetInt16(2),
			Count = reader.GetInt32(3),
		});
		
		await using var messageCmd = conn.Command(
			$"""
			 SELECT m.message_id, m.sender_id, m.channel_id, m.text, m.timestamp, met.edit_timestamp, mrt.replied_to_id
			 FROM messages m
			 LEFT JOIN message_edit_timestamps met ON m.message_id = met.message_id
			 LEFT JOIN message_replied_to mrt ON m.message_id = mrt.message_id
			 {filter.GenerateConditions("m").BuildWhereClause()}
			 """
		);
		
		await using var reader = await messageCmd.ExecuteReaderAsync(cancellationToken);
		
		while (await reader.ReadAsync(cancellationToken)) {
			ulong messageId = reader.GetUint64(0);
			
			yield return new Message {
				Id = messageId,
				Sender = reader.GetUint64(1),
				Channel = reader.GetUint64(2),
				Text = reader.GetString(3),
				Timestamp = reader.GetInt64(4),
				EditTimestamp = reader.IsDBNull(5) ? null : reader.GetInt64(5),
				RepliedToId = reader.IsDBNull(6) ? null : reader.GetUint64(6),
				Attachments = await attachmentCmd.GetItems(messageId),
				Embeds = await embedCmd.GetItems(messageId),
				Reactions = await reactionsCmd.GetItems(messageId),
			};
		}
	}
	
	public async IAsyncEnumerable<ulong> GetIds(MessageFilter? filter) {
		await using var conn = await pool.Take();
		
		await using var cmd = conn.Command("SELECT message_id FROM messages" + filter.GenerateConditions().BuildWhereClause());
		await using var reader = await cmd.ExecuteReaderAsync();
		
		while (await reader.ReadAsync()) {
			yield return reader.GetUint64(0);
		}
	}
	
	public async Task<int> Remove(MessageFilter filter, FilterRemovalMode mode) {
		int removed;
		await using (var conn = await pool.Take()) {
			removed = await conn.ExecuteAsync(
				          $"""
				           -- noinspection SqlWithoutWhere
				           DELETE FROM messages
				           {filter.GenerateConditions(invert: mode == FilterRemovalMode.KeepMatching).BuildWhereClause()}
				           """
			          );
		}
		
		UpdateTotalCount();
		return removed;
	}
	
	public async Task<int> RemoveUnreachableAttachments() {
		await using var conn = await pool.Take();
		return await conn.ExecuteAsync("DELETE FROM attachments WHERE attachment_id NOT IN (SELECT DISTINCT attachment_id FROM message_attachments)");
	}
}
