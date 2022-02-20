using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Utils.Collections;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite {
	public class SqliteDatabaseFile : IDatabaseFile {
		public static async Task<SqliteDatabaseFile?> OpenOrCreate(string path, Func<Task<bool>> checkCanUpgradeSchemas) {
			string connectionString = new SqliteConnectionStringBuilder {
				DataSource = path,
				Mode = SqliteOpenMode.ReadWriteCreate
			}.ToString();

			var conn = new SqliteConnection(connectionString);
			conn.Open();

			return await new Schema(conn).Setup(checkCanUpgradeSchemas) ? new SqliteDatabaseFile(path, conn) : null;
		}

		public string Path { get; }
		public DatabaseStatistics Statistics { get; }

		private readonly SqliteConnection conn;

		private SqliteDatabaseFile(string path, SqliteConnection conn) {
			this.conn = conn;
			this.Path = path;
			this.Statistics = new DatabaseStatistics();
			UpdateServerStatistics();
			UpdateChannelStatistics();
			UpdateUserStatistics();
			UpdateMessageStatistics();
		}

		public void Dispose() {
			conn.Dispose();
			GC.SuppressFinalize(this);
		}

		public void AddServer(Data.Server server) {
			using var cmd = conn.Upsert("servers", new[] {
				"id", "name", "type"
			});

			var serverParams = cmd.Parameters;
			serverParams.AddAndSet(":id", server.Id);
			serverParams.AddAndSet(":name", server.Name);
			serverParams.AddAndSet(":type", ServerTypes.ToString(server.Type));
			cmd.ExecuteNonQuery();
			UpdateServerStatistics();
		}

		public List<Data.Server> GetAllServers() {
			var list = new List<Data.Server>();

			using var cmd = conn.Command("SELECT id, name, type FROM servers");
			using var reader = cmd.ExecuteReader();

			while (reader.Read()) {
				list.Add(new Data.Server {
					Id = (ulong) reader.GetInt64(0),
					Name = reader.GetString(1),
					Type = ServerTypes.FromString(reader.GetString(2))
				});
			}

			return list;
		}

		public void AddChannel(Channel channel) {
			using var cmd = conn.Upsert("channels", new[] {
				"id", "server", "name", "parent_id", "position", "topic", "nsfw"
			});

			var channelParams = cmd.Parameters;
			channelParams.AddAndSet(":id", channel.Id);
			channelParams.AddAndSet(":server", channel.Server);
			channelParams.AddAndSet(":name", channel.Name);
			channelParams.AddAndSet(":parent_id", channel.ParentId);
			channelParams.AddAndSet(":position", channel.Position);
			channelParams.AddAndSet(":topic", channel.Topic);
			channelParams.AddAndSet(":nsfw", channel.Nsfw);
			cmd.ExecuteNonQuery();
			UpdateChannelStatistics();
		}

		public List<Channel> GetAllChannels() {
			var list = new List<Channel>();

			using var cmd = conn.Command("SELECT id, server, name, parent_id, position, topic, nsfw FROM channels");
			using var reader = cmd.ExecuteReader();

			while (reader.Read()) {
				list.Add(new Channel {
					Id = (ulong) reader.GetInt64(0),
					Server = (ulong) reader.GetInt64(1),
					Name = reader.GetString(2),
					ParentId = reader.IsDBNull(3) ? null : (ulong) reader.GetInt64(3),
					Position = reader.IsDBNull(4) ? null : reader.GetInt32(4),
					Topic = reader.IsDBNull(5) ? null : reader.GetString(5),
					Nsfw = reader.IsDBNull(6) ? null : reader.GetBoolean(6)
				});
			}

			return list;
		}

		public void AddUsers(User[] users) {
			using var tx = conn.BeginTransaction();
			using var cmd = conn.Upsert("users", new[] {
				"id", "name", "avatar_url", "discriminator"
			});

			var userParams = cmd.Parameters;
			userParams.Add(":id", SqliteType.Integer);
			userParams.Add(":name", SqliteType.Text);
			userParams.Add(":avatar_url", SqliteType.Text);
			userParams.Add(":discriminator", SqliteType.Text);

			foreach (var user in users) {
				userParams.Set(":id", user.Id);
				userParams.Set(":name", user.Name);
				userParams.Set(":avatar_url", user.AvatarUrl);
				userParams.Set(":discriminator", user.Discriminator);
				cmd.ExecuteNonQuery();
			}

			tx.Commit();
			UpdateUserStatistics();
		}

		public List<User> GetAllUsers() {
			var list = new List<User>();

			using var cmd = conn.Command("SELECT id, name, avatar_url, discriminator FROM users");
			using var reader = cmd.ExecuteReader();

			while (reader.Read()) {
				list.Add(new User {
					Id = (ulong) reader.GetInt64(0),
					Name = reader.GetString(1),
					AvatarUrl = reader.IsDBNull(2) ? null : reader.GetString(2),
					Discriminator = reader.IsDBNull(3) ? null : reader.GetString(3)
				});
			}

			return list;
		}

		public void AddMessages(Message[] messages) {
			using var tx = conn.BeginTransaction();
			using var messageCmd = conn.Upsert("messages", new[] {
				"message_id", "sender_id", "channel_id", "text", "timestamp", "edit_timestamp", "replied_to_id"
			});

			using var deleteAttachmentsCmd = conn.Command("DELETE FROM attachments WHERE message_id = :message_id");
			using var attachmentCmd = conn.Insert("attachments", new[] {
				"message_id", "attachment_id", "name", "type", "url", "size"
			});

			using var deleteEmbedsCmd = conn.Command("DELETE FROM embeds WHERE message_id = :message_id");
			using var embedCmd = conn.Insert("embeds", new[] {
				"message_id", "json"
			});

			using var deleteReactionsCmd = conn.Command("DELETE FROM reactions WHERE message_id = :message_id");
			using var reactionCmd = conn.Insert("reactions", new[] {
				"message_id", "emoji_id", "emoji_name", "emoji_flags", "count"
			});

			var messageParams = messageCmd.Parameters;
			messageParams.Add(":message_id", SqliteType.Integer);
			messageParams.Add(":sender_id", SqliteType.Integer);
			messageParams.Add(":channel_id", SqliteType.Integer);
			messageParams.Add(":text", SqliteType.Text);
			messageParams.Add(":timestamp", SqliteType.Integer);
			messageParams.Add(":edit_timestamp", SqliteType.Integer);
			messageParams.Add(":replied_to_id", SqliteType.Integer);

			var deleteAttachmentsParams = deleteAttachmentsCmd.Parameters;
			deleteAttachmentsParams.Add(":message_id", SqliteType.Integer);

			var attachmentParams = attachmentCmd.Parameters;
			attachmentParams.Add(":message_id", SqliteType.Integer);
			attachmentParams.Add(":attachment_id", SqliteType.Integer);
			attachmentParams.Add(":name", SqliteType.Text);
			attachmentParams.Add(":type", SqliteType.Text);
			attachmentParams.Add(":url", SqliteType.Text);
			attachmentParams.Add(":size", SqliteType.Integer);

			var deleteEmbedsParams = deleteEmbedsCmd.Parameters;
			deleteEmbedsParams.Add(":message_id", SqliteType.Integer);

			var embedParams = embedCmd.Parameters;
			embedParams.Add(":message_id", SqliteType.Integer);
			embedParams.Add(":json", SqliteType.Text);

			var deleteReactionsParams = deleteReactionsCmd.Parameters;
			deleteReactionsParams.Add(":message_id", SqliteType.Integer);

			var reactionParams = reactionCmd.Parameters;
			reactionParams.Add(":message_id", SqliteType.Integer);
			reactionParams.Add(":emoji_id", SqliteType.Integer);
			reactionParams.Add(":emoji_name", SqliteType.Text);
			reactionParams.Add(":emoji_flags", SqliteType.Integer);
			reactionParams.Add(":count", SqliteType.Integer);

			foreach (var message in messages) {
				object messageId = message.Id;

				messageParams.Set(":message_id", messageId);
				messageParams.Set(":sender_id", message.Sender);
				messageParams.Set(":channel_id", message.Channel);
				messageParams.Set(":text", message.Text);
				messageParams.Set(":timestamp", message.Timestamp);
				messageParams.Set(":edit_timestamp", message.EditTimestamp);
				messageParams.Set(":replied_to_id", message.RepliedToId);
				messageCmd.ExecuteNonQuery();

				deleteAttachmentsParams.Set(":message_id", messageId);
				deleteAttachmentsCmd.ExecuteNonQuery();

				deleteEmbedsParams.Set(":message_id", messageId);
				deleteEmbedsCmd.ExecuteNonQuery();

				deleteReactionsParams.Set(":message_id", messageId);
				deleteReactionsCmd.ExecuteNonQuery();

				if (!message.Attachments.IsEmpty) {
					foreach (var attachment in message.Attachments) {
						attachmentParams.Set(":message_id", messageId);
						attachmentParams.Set(":attachment_id", attachment.Id);
						attachmentParams.Set(":name", attachment.Name);
						attachmentParams.Set(":type", attachment.Type);
						attachmentParams.Set(":url", attachment.Url);
						attachmentParams.Set(":size", attachment.Size);
						attachmentCmd.ExecuteNonQuery();
					}
				}

				if (!message.Embeds.IsEmpty) {
					foreach (var embed in message.Embeds) {
						embedParams.Set(":message_id", messageId);
						embedParams.Set(":json", embed.Json);
						embedCmd.ExecuteNonQuery();
					}
				}

				if (!message.Reactions.IsEmpty) {
					foreach (var reaction in message.Reactions) {
						reactionParams.Set(":message_id", messageId);
						reactionParams.Set(":emoji_id", reaction.EmojiId);
						reactionParams.Set(":emoji_name", reaction.EmojiName);
						reactionParams.Set(":emoji_flags", (int) reaction.EmojiFlags);
						reactionParams.Set(":count", reaction.Count);
						reactionCmd.ExecuteNonQuery();
					}
				}
			}

			tx.Commit();
			UpdateMessageStatistics();
		}

		public int CountMessages(MessageFilter? filter = null) {
			using var cmd = conn.Command("SELECT COUNT(*) FROM messages" + filter.GenerateWhereClause());
			using var reader = cmd.ExecuteReader();

			return reader.Read() ? reader.GetInt32(0) : 0;
		}

		public List<Message> GetMessages(MessageFilter? filter = null) {
			var attachments = GetAllAttachments();
			var embeds = GetAllEmbeds();
			var reactions = GetAllReactions();

			var list = new List<Message>();

			using var cmd = conn.Command("SELECT message_id, sender_id, channel_id, text, timestamp, edit_timestamp, replied_to_id FROM messages" + filter.GenerateWhereClause());
			using var reader = cmd.ExecuteReader();

			while (reader.Read()) {
				ulong id = (ulong) reader.GetInt64(0);

				list.Add(new Message {
					Id = id,
					Sender = (ulong) reader.GetInt64(1),
					Channel = (ulong) reader.GetInt64(2),
					Text = reader.GetString(3),
					Timestamp = reader.GetInt64(4),
					EditTimestamp = reader.IsDBNull(5) ? null : reader.GetInt64(5),
					RepliedToId = reader.IsDBNull(6) ? null : (ulong) reader.GetInt64(6),
					Attachments = attachments.GetListOrNull(id)?.ToImmutableArray() ?? ImmutableArray<Attachment>.Empty,
					Embeds = embeds.GetListOrNull(id)?.ToImmutableArray() ?? ImmutableArray<Embed>.Empty,
					Reactions = reactions.GetListOrNull(id)?.ToImmutableArray() ?? ImmutableArray<Reaction>.Empty
				});
			}

			return list;
		}

		public void RemoveMessages(MessageFilter filter, MessageFilterRemovalMode mode) {
			var whereClause = filter.GenerateWhereClause(invert: mode == MessageFilterRemovalMode.KeepMatching);
			if (string.IsNullOrEmpty(whereClause)) {
				return;
			}

			// Rider is being stupid...
			StringBuilder build = new StringBuilder()
			                      .Append("DELETE ")
			                      .Append("FROM messages")
			                      .Append(whereClause);

			using var cmd = conn.Command(build.ToString());
			cmd.ExecuteNonQuery();

			UpdateMessageStatistics();
		}

		private MultiDictionary<ulong, Attachment> GetAllAttachments() {
			var dict = new MultiDictionary<ulong, Attachment>();

			using var cmd = conn.Command("SELECT message_id, attachment_id, name, type, url, size FROM attachments");
			using var reader = cmd.ExecuteReader();

			while (reader.Read()) {
				ulong messageId = (ulong) reader.GetInt64(0);

				dict.Add(messageId, new Attachment {
					Id = (ulong) reader.GetInt64(1),
					Name = reader.GetString(2),
					Type = reader.IsDBNull(3) ? null : reader.GetString(3),
					Url = reader.GetString(4),
					Size = (ulong) reader.GetInt64(5)
				});
			}

			return dict;
		}

		private MultiDictionary<ulong, Embed> GetAllEmbeds() {
			var dict = new MultiDictionary<ulong, Embed>();

			using var cmd = conn.Command("SELECT message_id, json FROM embeds");
			using var reader = cmd.ExecuteReader();

			while (reader.Read()) {
				ulong messageId = (ulong) reader.GetInt64(0);

				dict.Add(messageId, new Embed {
					Json = reader.GetString(1)
				});
			}

			return dict;
		}

		private MultiDictionary<ulong, Reaction> GetAllReactions() {
			var dict = new MultiDictionary<ulong, Reaction>();

			using var cmd = conn.Command("SELECT message_id, emoji_id, emoji_name, emoji_flags, count FROM reactions");
			using var reader = cmd.ExecuteReader();

			while (reader.Read()) {
				ulong messageId = (ulong) reader.GetInt64(0);

				dict.Add(messageId, new Reaction {
					EmojiId = reader.IsDBNull(1) ? null : (ulong) reader.GetInt64(1),
					EmojiName = reader.IsDBNull(2) ? null : reader.GetString(2),
					EmojiFlags = (EmojiFlags) reader.GetInt16(3),
					Count = reader.GetInt32(4)
				});
			}

			return dict;
		}

		private void UpdateServerStatistics() {
			using var cmd = conn.Command("SELECT COUNT(*) FROM servers");
			Statistics.TotalServers = cmd.ExecuteScalar() as long? ?? 0;
		}

		private void UpdateChannelStatistics() {
			using var cmd = conn.Command("SELECT COUNT(*) FROM channels");
			Statistics.TotalChannels = cmd.ExecuteScalar() as long? ?? 0;
		}

		private void UpdateUserStatistics() {
			using var cmd = conn.Command("SELECT COUNT(*) FROM users");
			Statistics.TotalUsers = cmd.ExecuteScalar() as long? ?? 0;
		}

		private void UpdateMessageStatistics() {
			using var cmd = conn.Command("SELECT COUNT(*) FROM messages");
			Statistics.TotalMessages = cmd.ExecuteScalar() as long? ?? 0L;
		}
	}
}
