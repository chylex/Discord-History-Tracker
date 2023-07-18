using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Aggregations;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Server.Download;
using DHT.Utils.Collections;
using DHT.Utils.Logging;
using DHT.Utils.Tasks;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite;

public sealed class SqliteDatabaseFile : IDatabaseFile {
	private const int DefaultPoolSize = 5;

	public static async Task<SqliteDatabaseFile?> OpenOrCreate(string path, Func<Task<bool>> checkCanUpgradeSchemas) {
		var connectionString = new SqliteConnectionStringBuilder {
			DataSource = path,
			Mode = SqliteOpenMode.ReadWriteCreate,
		};

		var pool = new SqliteConnectionPool(connectionString, DefaultPoolSize);
		bool wasOpened;

		using (var conn = pool.Take()) {
			wasOpened = await new Schema(conn).Setup(checkCanUpgradeSchemas);
		}

		if (wasOpened) {
			return new SqliteDatabaseFile(path, pool);
		}
		else {
			pool.Dispose();
			return null;
		}
	}

	public string Path { get; }
	public DatabaseStatistics Statistics { get; }

	private readonly Log log;
	private readonly SqliteConnectionPool pool;
	private readonly AsyncValueComputer<long>.Single totalMessagesComputer;
	private readonly AsyncValueComputer<long>.Single totalAttachmentsComputer;
	private readonly AsyncValueComputer<long>.Single totalDownloadsComputer;

	private SqliteDatabaseFile(string path, SqliteConnectionPool pool) {
		this.log = Log.ForType(typeof(SqliteDatabaseFile), System.IO.Path.GetFileName(path));
		this.pool = pool;

		this.totalMessagesComputer = AsyncValueComputer<long>.WithResultProcessor(UpdateMessageStatistics).WithOutdatedResults().BuildWithComputer(ComputeMessageStatistics);
		this.totalAttachmentsComputer = AsyncValueComputer<long>.WithResultProcessor(UpdateAttachmentStatistics).WithOutdatedResults().BuildWithComputer(ComputeAttachmentStatistics);
		this.totalDownloadsComputer = AsyncValueComputer<long>.WithResultProcessor(UpdateDownloadStatistics).WithOutdatedResults().BuildWithComputer(ComputeDownloadStatistics);

		this.Path = path;
		this.Statistics = new DatabaseStatistics();

		using (var conn = pool.Take()) {
			UpdateServerStatistics(conn);
			UpdateChannelStatistics(conn);
			UpdateUserStatistics(conn);
		}

		totalMessagesComputer.Recompute();
		totalAttachmentsComputer.Recompute();
		totalDownloadsComputer.Recompute();
	}

	public void Dispose() {
		pool.Dispose();
	}

	public DatabaseStatisticsSnapshot SnapshotStatistics() {
		return new DatabaseStatisticsSnapshot {
			TotalServers = Statistics.TotalServers,
			TotalChannels = Statistics.TotalChannels,
			TotalUsers = Statistics.TotalUsers,
			TotalMessages = ComputeMessageStatistics(),
		};
	}

	public void AddServer(Data.Server server) {
		using var conn = pool.Take();
		using var cmd = conn.Upsert("servers", new[] {
			("id", SqliteType.Integer),
			("name", SqliteType.Text),
			("type", SqliteType.Text),
		});

		cmd.Set(":id", server.Id);
		cmd.Set(":name", server.Name);
		cmd.Set(":type", ServerTypes.ToString(server.Type));
		cmd.ExecuteNonQuery();
		UpdateServerStatistics(conn);
	}

	public List<Data.Server> GetAllServers() {
		var perf = log.Start();
		var list = new List<Data.Server>();

		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT id, name, type FROM servers");
		using var reader = cmd.ExecuteReader();

		while (reader.Read()) {
			list.Add(new Data.Server {
				Id = reader.GetUint64(0),
				Name = reader.GetString(1),
				Type = ServerTypes.FromString(reader.GetString(2)),
			});
		}

		perf.End();
		return list;
	}

	public void AddChannel(Channel channel) {
		using var conn = pool.Take();
		using var cmd = conn.Upsert("channels", new[] {
			("id", SqliteType.Integer),
			("server", SqliteType.Integer),
			("name", SqliteType.Text),
			("parent_id", SqliteType.Integer),
			("position", SqliteType.Integer),
			("topic", SqliteType.Text),
			("nsfw", SqliteType.Integer),
		});

		cmd.Set(":id", channel.Id);
		cmd.Set(":server", channel.Server);
		cmd.Set(":name", channel.Name);
		cmd.Set(":parent_id", channel.ParentId);
		cmd.Set(":position", channel.Position);
		cmd.Set(":topic", channel.Topic);
		cmd.Set(":nsfw", channel.Nsfw);
		cmd.ExecuteNonQuery();
		UpdateChannelStatistics(conn);
	}

	public List<Channel> GetAllChannels() {
		var list = new List<Channel>();

		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT id, server, name, parent_id, position, topic, nsfw FROM channels");
		using var reader = cmd.ExecuteReader();

		while (reader.Read()) {
			list.Add(new Channel {
				Id = reader.GetUint64(0),
				Server = reader.GetUint64(1),
				Name = reader.GetString(2),
				ParentId = reader.IsDBNull(3) ? null : reader.GetUint64(3),
				Position = reader.IsDBNull(4) ? null : reader.GetInt32(4),
				Topic = reader.IsDBNull(5) ? null : reader.GetString(5),
				Nsfw = reader.IsDBNull(6) ? null : reader.GetBoolean(6),
			});
		}

		return list;
	}

	public void AddUsers(User[] users) {
		using var conn = pool.Take();
		using var tx = conn.BeginTransaction();
		using var cmd = conn.Upsert("users", new[] {
			("id", SqliteType.Integer),
			("name", SqliteType.Text),
			("avatar_url", SqliteType.Text),
			("discriminator", SqliteType.Text),
		});

		foreach (var user in users) {
			cmd.Set(":id", user.Id);
			cmd.Set(":name", user.Name);
			cmd.Set(":avatar_url", user.AvatarUrl);
			cmd.Set(":discriminator", user.Discriminator);
			cmd.ExecuteNonQuery();
		}

		tx.Commit();
		UpdateUserStatistics(conn);
	}

	public List<User> GetAllUsers() {
		var perf = log.Start();
		var list = new List<User>();

		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT id, name, avatar_url, discriminator FROM users");
		using var reader = cmd.ExecuteReader();

		while (reader.Read()) {
			list.Add(new User {
				Id = reader.GetUint64(0),
				Name = reader.GetString(1),
				AvatarUrl = reader.IsDBNull(2) ? null : reader.GetString(2),
				Discriminator = reader.IsDBNull(3) ? null : reader.GetString(3),
			});
		}

		perf.End();
		return list;
	}

	public void AddMessages(Message[] messages) {
		static SqliteCommand DeleteByMessageId(ISqliteConnection conn, string tableName) {
			return conn.Delete(tableName, ("message_id", SqliteType.Integer));
		}

		static void ExecuteDeleteByMessageId(SqliteCommand cmd, object id) {
			cmd.Set(":message_id", id);
			cmd.ExecuteNonQuery();
		}

		bool addedAttachments = false;

		using (var conn = pool.Take()) {
			using var tx = conn.BeginTransaction();

			using var messageCmd = conn.Upsert("messages", new[] {
				("message_id", SqliteType.Integer),
				("sender_id", SqliteType.Integer),
				("channel_id", SqliteType.Integer),
				("text", SqliteType.Text),
				("timestamp", SqliteType.Integer),
			});

			using var deleteEditTimestampCmd = DeleteByMessageId(conn, "edit_timestamps");
			using var deleteRepliedToCmd = DeleteByMessageId(conn, "replied_to");

			using var deleteAttachmentsCmd = DeleteByMessageId(conn, "attachments");
			using var deleteEmbedsCmd = DeleteByMessageId(conn, "embeds");
			using var deleteReactionsCmd = DeleteByMessageId(conn, "reactions");

			using var editTimestampCmd = conn.Insert("edit_timestamps", new [] {
				("message_id", SqliteType.Integer),
				("edit_timestamp", SqliteType.Integer),
			});

			using var repliedToCmd = conn.Insert("replied_to", new [] {
				("message_id", SqliteType.Integer),
				("replied_to_id", SqliteType.Integer),
			});

			using var attachmentCmd = conn.Insert("attachments", new[] {
				("message_id", SqliteType.Integer),
				("attachment_id", SqliteType.Integer),
				("name", SqliteType.Text),
				("type", SqliteType.Text),
				("url", SqliteType.Text),
				("size", SqliteType.Integer),
				("width", SqliteType.Integer),
				("height", SqliteType.Integer),
			});

			using var embedCmd = conn.Insert("embeds", new[] {
				("message_id", SqliteType.Integer),
				("json", SqliteType.Text),
			});

			using var reactionCmd = conn.Insert("reactions", new[] {
				("message_id", SqliteType.Integer),
				("emoji_id", SqliteType.Integer),
				("emoji_name", SqliteType.Text),
				("emoji_flags", SqliteType.Integer),
				("count", SqliteType.Integer),
			});

			foreach (var message in messages) {
				object messageId = message.Id;

				messageCmd.Set(":message_id", messageId);
				messageCmd.Set(":sender_id", message.Sender);
				messageCmd.Set(":channel_id", message.Channel);
				messageCmd.Set(":text", message.Text);
				messageCmd.Set(":timestamp", message.Timestamp);
				messageCmd.ExecuteNonQuery();

				ExecuteDeleteByMessageId(deleteEditTimestampCmd, messageId);
				ExecuteDeleteByMessageId(deleteRepliedToCmd, messageId);

				ExecuteDeleteByMessageId(deleteAttachmentsCmd, messageId);
				ExecuteDeleteByMessageId(deleteEmbedsCmd, messageId);
				ExecuteDeleteByMessageId(deleteReactionsCmd, messageId);

				if (message.EditTimestamp is {} timestamp) {
					editTimestampCmd.Set(":message_id", messageId);
					editTimestampCmd.Set(":edit_timestamp", timestamp);
					editTimestampCmd.ExecuteNonQuery();
				}

				if (message.RepliedToId is {} repliedToId) {
					repliedToCmd.Set(":message_id", messageId);
					repliedToCmd.Set(":replied_to_id", repliedToId);
					repliedToCmd.ExecuteNonQuery();
				}

				if (!message.Attachments.IsEmpty) {
					addedAttachments = true;

					foreach (var attachment in message.Attachments) {
						attachmentCmd.Set(":message_id", messageId);
						attachmentCmd.Set(":attachment_id", attachment.Id);
						attachmentCmd.Set(":name", attachment.Name);
						attachmentCmd.Set(":type", attachment.Type);
						attachmentCmd.Set(":url", attachment.Url);
						attachmentCmd.Set(":size", attachment.Size);
						attachmentCmd.Set(":width", attachment.Width);
						attachmentCmd.Set(":height", attachment.Height);
						attachmentCmd.ExecuteNonQuery();
					}
				}

				if (!message.Embeds.IsEmpty) {
					foreach (var embed in message.Embeds) {
						embedCmd.Set(":message_id", messageId);
						embedCmd.Set(":json", embed.Json);
						embedCmd.ExecuteNonQuery();
					}
				}

				if (!message.Reactions.IsEmpty) {
					foreach (var reaction in message.Reactions) {
						reactionCmd.Set(":message_id", messageId);
						reactionCmd.Set(":emoji_id", reaction.EmojiId);
						reactionCmd.Set(":emoji_name", reaction.EmojiName);
						reactionCmd.Set(":emoji_flags", (int) reaction.EmojiFlags);
						reactionCmd.Set(":count", reaction.Count);
						reactionCmd.ExecuteNonQuery();
					}
				}
			}

			tx.Commit();
		}

		totalMessagesComputer.Recompute();

		if (addedAttachments) {
			totalAttachmentsComputer.Recompute();
		}
	}

	public int CountMessages(MessageFilter? filter = null) {
		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT COUNT(*) FROM messages" + filter.GenerateWhereClause());
		using var reader = cmd.ExecuteReader();

		return reader.Read() ? reader.GetInt32(0) : 0;
	}

	public List<Message> GetMessages(MessageFilter? filter = null) {
		var perf = log.Start();
		var list = new List<Message>();

		var attachments = GetAllAttachments();
		var embeds = GetAllEmbeds();
		var reactions = GetAllReactions();

		using var conn = pool.Take();
		using var cmd = conn.Command(@"
SELECT m.message_id, m.sender_id, m.channel_id, m.text, m.timestamp, et.edit_timestamp, rt.replied_to_id
FROM messages m
LEFT JOIN edit_timestamps et ON m.message_id = et.message_id
LEFT JOIN replied_to rt ON m.message_id = rt.message_id" + filter.GenerateWhereClause("m"));
		using var reader = cmd.ExecuteReader();

		while (reader.Read()) {
			ulong id = reader.GetUint64(0);

			list.Add(new Message {
				Id = id,
				Sender = reader.GetUint64(1),
				Channel = reader.GetUint64(2),
				Text = reader.GetString(3),
				Timestamp = reader.GetInt64(4),
				EditTimestamp = reader.IsDBNull(5) ? null : reader.GetInt64(5),
				RepliedToId = reader.IsDBNull(6) ? null : reader.GetUint64(6),
				Attachments = attachments.GetListOrNull(id)?.ToImmutableArray() ?? ImmutableArray<Attachment>.Empty,
				Embeds = embeds.GetListOrNull(id)?.ToImmutableArray() ?? ImmutableArray<Embed>.Empty,
				Reactions = reactions.GetListOrNull(id)?.ToImmutableArray() ?? ImmutableArray<Reaction>.Empty,
			});
		}

		perf.End();
		return list;
	}

	public HashSet<ulong> GetMessageIds(MessageFilter? filter = null) {
		var perf = log.Start();
		var ids = new HashSet<ulong>();

		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT message_id FROM messages" + filter.GenerateWhereClause());
		using var reader = cmd.ExecuteReader();

		while (reader.Read()) {
			ids.Add(reader.GetUint64(0));
		}

		perf.End();
		return ids;
	}

	public void RemoveMessages(MessageFilter filter, FilterRemovalMode mode) {
		var perf = log.Start();

		DeleteFromTable("messages", filter.GenerateWhereClause(invert: mode == FilterRemovalMode.KeepMatching));
		totalMessagesComputer.Recompute();

		perf.End();
	}

	public int CountAttachments(AttachmentFilter? filter = null) {
		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT COUNT(DISTINCT url) FROM attachments a" + filter.GenerateWhereClause("a"));
		using var reader = cmd.ExecuteReader();

		return reader.Read() ? reader.GetInt32(0) : 0;
	}

	public void AddDownload(Data.Download download) {
		using var conn = pool.Take();
		using var cmd = conn.Upsert("downloads", new[] {
			("url", SqliteType.Text),
			("status", SqliteType.Integer),
			("size", SqliteType.Integer),
			("blob", SqliteType.Blob),
		});

		cmd.Set(":url", download.Url);
		cmd.Set(":status", (int) download.Status);
		cmd.Set(":size", download.Size);
		cmd.Set(":blob", download.Data);
		cmd.ExecuteNonQuery();

		totalDownloadsComputer.Recompute();
	}

	public List<Data.Download> GetDownloadsWithoutData() {
		var list = new List<Data.Download>();

		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT url, status, size FROM downloads");
		using var reader = cmd.ExecuteReader();

		while (reader.Read()) {
			string url = reader.GetString(0);
			var status = (DownloadStatus) reader.GetInt32(1);
			ulong size = reader.GetUint64(2);

			list.Add(new Data.Download(url, status, size));
		}

		return list;
	}

	public Data.Download GetDownloadWithData(Data.Download download) {
		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT blob FROM downloads WHERE url = :url");
		cmd.AddAndSet(":url", SqliteType.Text, download.Url);

		using var reader = cmd.ExecuteReader();

		if (reader.Read() && !reader.IsDBNull(0)) {
			return download.WithData((byte[]) reader["blob"]);
		}
		else {
			return download;
		}
	}

	public DownloadedAttachment? GetDownloadedAttachment(string url) {
		using var conn = pool.Take();
		using var cmd = conn.Command(@"
SELECT a.type, d.blob FROM downloads d
LEFT JOIN attachments a ON d.url = a.url
WHERE d.url = :url AND d.status = :success AND d.blob IS NOT NULL");

		cmd.AddAndSet(":url", SqliteType.Text, url);
		cmd.AddAndSet(":success", SqliteType.Integer, (int) DownloadStatus.Success);

		using var reader = cmd.ExecuteReader();

		if (!reader.Read()) {
			return null;
		}

		return new DownloadedAttachment {
			Type = reader.IsDBNull(0) ? null : reader.GetString(0),
			Data = (byte[]) reader["blob"],
		};
	}

	public void EnqueueDownloadItems(AttachmentFilter? filter = null) {
		using var conn = pool.Take();
		using var cmd = conn.Command("INSERT INTO downloads (url, status, size) SELECT a.url, :enqueued, MAX(a.size) FROM attachments a" + filter.GenerateWhereClause("a") + " GROUP BY a.url");
		cmd.AddAndSet(":enqueued", SqliteType.Integer, (int) DownloadStatus.Enqueued);
		cmd.ExecuteNonQuery();
	}

	public List<DownloadItem> GetEnqueuedDownloadItems(int count) {
		var list = new List<DownloadItem>();

		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT url, size FROM downloads WHERE status = :enqueued LIMIT :limit");
		cmd.AddAndSet(":enqueued", SqliteType.Integer, (int) DownloadStatus.Enqueued);
		cmd.AddAndSet(":limit", SqliteType.Integer, Math.Max(0, count));

		using var reader = cmd.ExecuteReader();

		while (reader.Read()) {
			list.Add(new DownloadItem {
				Url = reader.GetString(0),
				Size = reader.GetUint64(1),
			});
		}

		return list;
	}

	public void RemoveDownloadItems(DownloadItemFilter? filter, FilterRemovalMode mode) {
		DeleteFromTable("downloads", filter.GenerateWhereClause(invert: mode == FilterRemovalMode.KeepMatching));
		totalDownloadsComputer.Recompute();
	}

	public DownloadStatusStatistics GetDownloadStatusStatistics() {
		static void LoadUndownloadedStatistics(ISqliteConnection conn, DownloadStatusStatistics result) {
			using var cmd = conn.Command("SELECT IFNULL(COUNT(size), 0), IFNULL(SUM(size), 0) FROM (SELECT MAX(a.size) size FROM attachments a WHERE a.url NOT IN (SELECT d.url FROM downloads d) GROUP BY a.url)");
			using var reader = cmd.ExecuteReader();

			if (reader.Read()) {
				result.SkippedCount = reader.GetInt32(0);
				result.SkippedSize = reader.GetUint64(1);
			}
		}

		static void LoadSuccessStatistics(ISqliteConnection conn, DownloadStatusStatistics result) {
			using var cmd = conn.Command(@"SELECT
IFNULL(SUM(CASE WHEN status = :enqueued THEN 1 ELSE 0 END), 0),
IFNULL(SUM(CASE WHEN status = :enqueued THEN size ELSE 0 END), 0),
IFNULL(SUM(CASE WHEN status = :success THEN 1 ELSE 0 END), 0),
IFNULL(SUM(CASE WHEN status = :success THEN size ELSE 0 END), 0),
IFNULL(SUM(CASE WHEN status != :enqueued AND status != :success THEN 1 ELSE 0 END), 0),
IFNULL(SUM(CASE WHEN status != :enqueued AND status != :success THEN size ELSE 0 END), 0)
FROM downloads");
			cmd.AddAndSet(":enqueued", SqliteType.Integer, (int) DownloadStatus.Enqueued);
			cmd.AddAndSet(":success", SqliteType.Integer, (int) DownloadStatus.Success);

			using var reader = cmd.ExecuteReader();

			if (reader.Read()) {
				result.EnqueuedCount = reader.GetInt32(0);
				result.EnqueuedSize = reader.GetUint64(1);
				result.SuccessfulCount = reader.GetInt32(2);
				result.SuccessfulSize = reader.GetUint64(3);
				result.FailedCount = reader.GetInt32(4);
				result.FailedSize = reader.GetUint64(5);
			}
		}

		var result = new DownloadStatusStatistics();

		using var conn = pool.Take();
		LoadUndownloadedStatistics(conn, result);
		LoadSuccessStatistics(conn, result);
		return result;
	}

	private MultiDictionary<ulong, Attachment> GetAllAttachments() {
		var dict = new MultiDictionary<ulong, Attachment>();

		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT message_id, attachment_id, name, type, url, size, width, height FROM attachments");
		using var reader = cmd.ExecuteReader();

		while (reader.Read()) {
			ulong messageId = reader.GetUint64(0);

			dict.Add(messageId, new Attachment {
				Id = reader.GetUint64(1),
				Name = reader.GetString(2),
				Type = reader.IsDBNull(3) ? null : reader.GetString(3),
				Url = reader.GetString(4),
				Size = reader.GetUint64(5),
				Width = reader.IsDBNull(6) ? null : reader.GetInt32(6),
				Height = reader.IsDBNull(7) ? null : reader.GetInt32(7),
			});
		}

		return dict;
	}

	private MultiDictionary<ulong, Embed> GetAllEmbeds() {
		var dict = new MultiDictionary<ulong, Embed>();

		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT message_id, json FROM embeds");
		using var reader = cmd.ExecuteReader();

		while (reader.Read()) {
			ulong messageId = reader.GetUint64(0);

			dict.Add(messageId, new Embed {
				Json = reader.GetString(1),
			});
		}

		return dict;
	}

	private MultiDictionary<ulong, Reaction> GetAllReactions() {
		var dict = new MultiDictionary<ulong, Reaction>();

		using var conn = pool.Take();
		using var cmd = conn.Command("SELECT message_id, emoji_id, emoji_name, emoji_flags, count FROM reactions");
		using var reader = cmd.ExecuteReader();

		while (reader.Read()) {
			ulong messageId = reader.GetUint64(0);

			dict.Add(messageId, new Reaction {
				EmojiId = reader.IsDBNull(1) ? null : reader.GetUint64(1),
				EmojiName = reader.IsDBNull(2) ? null : reader.GetString(2),
				EmojiFlags = (EmojiFlags) reader.GetInt16(3),
				Count = reader.GetInt32(4),
			});
		}

		return dict;
	}

	private void DeleteFromTable(string table, string whereClause) {
		// Rider is being stupid...
		StringBuilder build = new StringBuilder()
		                      .Append("DELETE ")
		                      .Append("FROM ")
		                      .Append(table)
		                      .Append(whereClause);

		using var conn = pool.Take();
		using var cmd = conn.Command(build.ToString());
		cmd.ExecuteNonQuery();
	}

	public void Vacuum() {
		using var conn = pool.Take();
		using var cmd = conn.Command("VACUUM");
		cmd.ExecuteNonQuery();
	}

	private void UpdateServerStatistics(ISqliteConnection conn) {
		Statistics.TotalServers = conn.SelectScalar("SELECT COUNT(*) FROM servers") as long? ?? 0;
	}

	private void UpdateChannelStatistics(ISqliteConnection conn) {
		Statistics.TotalChannels = conn.SelectScalar("SELECT COUNT(*) FROM channels") as long? ?? 0;
	}

	private void UpdateUserStatistics(ISqliteConnection conn) {
		Statistics.TotalUsers = conn.SelectScalar("SELECT COUNT(*) FROM users") as long? ?? 0;
	}

	private long ComputeMessageStatistics() {
		using var conn = pool.Take();
		return conn.SelectScalar("SELECT COUNT(*) FROM messages") as long? ?? 0L;
	}

	private void UpdateMessageStatistics(long totalMessages) {
		Statistics.TotalMessages = totalMessages;
	}

	private long ComputeAttachmentStatistics() {
		using var conn = pool.Take();
		return conn.SelectScalar("SELECT COUNT(DISTINCT url) FROM attachments") as long? ?? 0L;
	}

	private void UpdateAttachmentStatistics(long totalAttachments) {
		Statistics.TotalAttachments = totalAttachments;
	}

	private long ComputeDownloadStatistics() {
		using var conn = pool.Take();
		return conn.SelectScalar("SELECT COUNT(*) FROM downloads") as long? ?? 0L;
	}

	private void UpdateDownloadStatistics(long totalDownloads) {
		Statistics.TotalDownloads = totalDownloads;
	}
}
