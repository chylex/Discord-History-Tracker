using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Server.Download;
using DHT.Utils.Logging;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Repositories;

sealed class SqliteMessageRepository : BaseSqliteRepository, IMessageRepository {
	private static readonly Log Log = Log.ForType<SqliteMessageRepository>();
	
	private readonly SqliteConnectionPool pool;
	private readonly SqliteDownloadRepository downloads;

	public SqliteMessageRepository(SqliteConnectionPool pool, SqliteDownloadRepository downloads) : base(Log) {
		this.pool = pool;
		this.downloads = downloads;
	}

	public async Task Add(IReadOnlyList<Message> messages) {
		if (messages.Count == 0) {
			return;
		}

		static SqliteCommand DeleteByMessageId(ISqliteConnection conn, string tableName) {
			return conn.Delete(tableName, ("message_id", SqliteType.Integer));
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
				("timestamp", SqliteType.Integer)
			]);

			await using var deleteEditTimestampCmd = DeleteByMessageId(conn, "edit_timestamps");
			await using var deleteRepliedToCmd = DeleteByMessageId(conn, "replied_to");

			await using var deleteAttachmentsCmd = DeleteByMessageId(conn, "attachments");
			await using var deleteEmbedsCmd = DeleteByMessageId(conn, "embeds");
			await using var deleteReactionsCmd = DeleteByMessageId(conn, "reactions");

			await using var editTimestampCmd = conn.Insert("edit_timestamps", [
				("message_id", SqliteType.Integer),
				("edit_timestamp", SqliteType.Integer)
			]);

			await using var repliedToCmd = conn.Insert("replied_to", [
				("message_id", SqliteType.Integer),
				("replied_to_id", SqliteType.Integer)
			]);

			await using var attachmentCmd = conn.Insert("attachments", [
				("message_id", SqliteType.Integer),
				("attachment_id", SqliteType.Integer),
				("name", SqliteType.Text),
				("type", SqliteType.Text),
				("normalized_url", SqliteType.Text),
				("download_url", SqliteType.Text),
				("size", SqliteType.Integer),
				("width", SqliteType.Integer),
				("height", SqliteType.Integer)
			]);

			await using var embedCmd = conn.Insert("embeds", [
				("message_id", SqliteType.Integer),
				("json", SqliteType.Text)
			]);

			await using var reactionCmd = conn.Insert("reactions", [
				("message_id", SqliteType.Integer),
				("emoji_id", SqliteType.Integer),
				("emoji_name", SqliteType.Text),
				("emoji_flags", SqliteType.Integer),
				("count", SqliteType.Integer)
			]);
			
			await using var downloadCollector = new SqliteDownloadRepository.NewDownloadCollector(downloads, conn);

			foreach (var message in messages) {
				object messageId = message.Id;

				messageCmd.Set(":message_id", messageId);
				messageCmd.Set(":sender_id", message.Sender);
				messageCmd.Set(":channel_id", message.Channel);
				messageCmd.Set(":text", message.Text);
				messageCmd.Set(":timestamp", message.Timestamp);
				await messageCmd.ExecuteNonQueryAsync();

				await ExecuteDeleteByMessageId(deleteEditTimestampCmd, messageId);
				await ExecuteDeleteByMessageId(deleteRepliedToCmd, messageId);

				await ExecuteDeleteByMessageId(deleteAttachmentsCmd, messageId);
				await ExecuteDeleteByMessageId(deleteEmbedsCmd, messageId);
				await ExecuteDeleteByMessageId(deleteReactionsCmd, messageId);

				if (message.EditTimestamp is {} timestamp) {
					editTimestampCmd.Set(":message_id", messageId);
					editTimestampCmd.Set(":edit_timestamp", timestamp);
					await editTimestampCmd.ExecuteNonQueryAsync();
				}

				if (message.RepliedToId is {} repliedToId) {
					repliedToCmd.Set(":message_id", messageId);
					repliedToCmd.Set(":replied_to_id", repliedToId);
					await repliedToCmd.ExecuteNonQueryAsync();
				}

				if (!message.Attachments.IsEmpty) {
					foreach (var attachment in message.Attachments) {
						attachmentCmd.Set(":message_id", messageId);
						attachmentCmd.Set(":attachment_id", attachment.Id);
						attachmentCmd.Set(":name", attachment.Name);
						attachmentCmd.Set(":type", attachment.Type);
						attachmentCmd.Set(":normalized_url", attachment.NormalizedUrl);
						attachmentCmd.Set(":download_url", attachment.DownloadUrl);
						attachmentCmd.Set(":size", attachment.Size);
						attachmentCmd.Set(":width", attachment.Width);
						attachmentCmd.Set(":height", attachment.Height);
						await attachmentCmd.ExecuteNonQueryAsync();
						
						await downloadCollector.Add(DownloadLinkExtractor.FromAttachment(attachment));
					}
				}

				if (!message.Embeds.IsEmpty) {
					foreach (var embed in message.Embeds) {
						embedCmd.Set(":message_id", messageId);
						embedCmd.Set(":json", embed.Json);
						await embedCmd.ExecuteNonQueryAsync();

						if (DownloadLinkExtractor.TryFromEmbedJson(embed.Json) is {} download) {
							await downloadCollector.Add(download);
						}
					}
				}

				if (!message.Reactions.IsEmpty) {
					foreach (var reaction in message.Reactions) {
						reactionCmd.Set(":message_id", messageId);
						reactionCmd.Set(":emoji_id", reaction.EmojiId);
						reactionCmd.Set(":emoji_name", reaction.EmojiName);
						reactionCmd.Set(":emoji_flags", (int) reaction.EmojiFlags);
						reactionCmd.Set(":count", reaction.Count);
						await reactionCmd.ExecuteNonQueryAsync();

						if (reaction.EmojiId is {} emojiId) {
							await downloadCollector.Add(DownloadLinkExtractor.FromEmoji(emojiId, reaction.EmojiFlags));
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

	private sealed class MesageToManyCommand<T> : IAsyncDisposable {
		private readonly SqliteCommand cmd;
		private readonly Func<SqliteDataReader, T> readItem;

		public MesageToManyCommand(ISqliteConnection conn, string sql, Func<SqliteDataReader, T> readItem) {
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

	public async IAsyncEnumerable<Message> Get(MessageFilter? filter) {
		await using var conn = await pool.Take();

		const string AttachmentSql =
			"""
			SELECT attachment_id, name, type, normalized_url, download_url, size, width, height
			FROM attachments
			WHERE message_id = :message_id
			""";

		await using var attachmentCmd = new MesageToManyCommand<Attachment>(conn, AttachmentSql, static reader => new Attachment {
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
			FROM embeds
			WHERE message_id = :message_id
			""";

		await using var embedCmd = new MesageToManyCommand<Embed>(conn, EmbedSql, static reader => new Embed {
			Json = reader.GetString(0)
		});

		const string ReactionSql =
			"""
			SELECT emoji_id, emoji_name, emoji_flags, count
			FROM reactions
			WHERE message_id = :message_id
			""";

		await using var reactionsCmd = new MesageToManyCommand<Reaction>(conn, ReactionSql, static reader => new Reaction {
			EmojiId = reader.IsDBNull(0) ? null : reader.GetUint64(0),
			EmojiName = reader.IsDBNull(1) ? null : reader.GetString(1),
			EmojiFlags = (EmojiFlags) reader.GetInt16(2),
			Count = reader.GetInt32(3),
		});

		await using var messageCmd = conn.Command(
			$"""
			 SELECT m.message_id, m.sender_id, m.channel_id, m.text, m.timestamp, et.edit_timestamp, rt.replied_to_id
			 FROM messages m
			 LEFT JOIN edit_timestamps et ON m.message_id = et.message_id
			 LEFT JOIN replied_to rt ON m.message_id = rt.message_id
			 {filter.GenerateConditions("m").BuildWhereClause()}
			 """
		);

		await using var reader = await messageCmd.ExecuteReaderAsync();

		while (await reader.ReadAsync()) {
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
				Reactions = await reactionsCmd.GetItems(messageId)
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

	public async Task Remove(MessageFilter filter, FilterRemovalMode mode) {
		await using (var conn = await pool.Take()) {
			await conn.ExecuteAsync(
				$"""
				 -- noinspection SqlWithoutWhere
				 DELETE FROM messages
				 {filter.GenerateConditions(invert: mode == FilterRemovalMode.KeepMatching).BuildWhereClause()}
				 """
			);
		}

		UpdateTotalCount();
	}
}
