using System;
using System.Threading.Tasks;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Repositories;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Utils.Tasks;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite;

public sealed class SqliteDatabaseFile : IDatabaseFile {
	private const int DefaultPoolSize = 5;

	public static async Task<SqliteDatabaseFile?> OpenOrCreate(string path, ISchemaUpgradeCallbacks schemaUpgradeCallbacks, TaskScheduler computeTaskResultScheduler) {
		var connectionString = new SqliteConnectionStringBuilder {
			DataSource = path,
			Mode = SqliteOpenMode.ReadWriteCreate,
		};

		var pool = new SqliteConnectionPool(connectionString, DefaultPoolSize);
		bool wasOpened;

		try {
			using var conn = pool.Take();
			wasOpened = await new Schema(conn).Setup(schemaUpgradeCallbacks);
		} catch (Exception) {
			pool.Dispose();
			throw;
		}

		if (wasOpened) {
			var db = new SqliteDatabaseFile(path, pool, computeTaskResultScheduler);
			await db.Initialize();
			return db;
		}
		else {
			pool.Dispose();
			return null;
		}
	}

	public string Path { get; }
	public DatabaseStatistics Statistics { get; }
	
	public IUserRepository Users => users;
	public IServerRepository Servers => servers;
	public IChannelRepository Channels => channels;
	public IMessageRepository Messages => messages;
	public IDownloadRepository Downloads => downloads;
	
	private readonly SqliteConnectionPool pool;
	
	private readonly SqliteUserRepository users;
	private readonly SqliteServerRepository servers;
	private readonly SqliteChannelRepository channels;
	private readonly SqliteMessageRepository messages;
	private readonly SqliteDownloadRepository downloads;
	
	private readonly AsyncValueComputer<long>.Single totalMessagesComputer;
	private readonly AsyncValueComputer<long>.Single totalAttachmentsComputer;
	private readonly AsyncValueComputer<long>.Single totalDownloadsComputer;

	private SqliteDatabaseFile(string path, SqliteConnectionPool pool, TaskScheduler computeTaskResultScheduler) {
		this.pool = pool;

		this.totalMessagesComputer = AsyncValueComputer<long>.WithResultProcessor(UpdateMessageStatistics, computeTaskResultScheduler).WithOutdatedResults().BuildWithComputer(ComputeMessageStatistics);
		this.totalAttachmentsComputer = AsyncValueComputer<long>.WithResultProcessor(UpdateAttachmentStatistics, computeTaskResultScheduler).WithOutdatedResults().BuildWithComputer(ComputeAttachmentStatistics);
		this.totalDownloadsComputer = AsyncValueComputer<long>.WithResultProcessor(UpdateDownloadStatistics, computeTaskResultScheduler).WithOutdatedResults().BuildWithComputer(ComputeDownloadStatistics);

		this.Path = path;
		this.Statistics = new DatabaseStatistics();

		this.users = new SqliteUserRepository(pool, Statistics);
		this.servers = new SqliteServerRepository(pool, Statistics);
		this.channels = new SqliteChannelRepository(pool, Statistics);
		this.messages = new SqliteMessageRepository(pool, totalMessagesComputer, totalAttachmentsComputer);
		this.downloads = new SqliteDownloadRepository(pool, totalDownloadsComputer);

		totalMessagesComputer.Recompute();
		totalAttachmentsComputer.Recompute();
		totalDownloadsComputer.Recompute();
	}

	private async Task Initialize() {
		await users.Initialize();
		await servers.Initialize();
		await channels.Initialize();
	}

	public void Dispose() {
		totalMessagesComputer.Cancel();
		totalAttachmentsComputer.Cancel();
		totalDownloadsComputer.Cancel();
		pool.Dispose();
	}

	public async Task<DatabaseStatisticsSnapshot> SnapshotStatistics() {
		return new DatabaseStatisticsSnapshot {
			TotalServers = Statistics.TotalServers,
			TotalChannels = Statistics.TotalChannels,
			TotalUsers = Statistics.TotalUsers,
			TotalMessages = await ComputeMessageStatistics(),
		};
	}

	public async Task Vacuum() {
		using var conn = pool.Take();
		await conn.ExecuteAsync("VACUUM");
	}

	private async Task<long> ComputeMessageStatistics() {
		using var conn = pool.Take();
		return await conn.ExecuteReaderAsync("SELECT COUNT(*) FROM messages", static reader => reader?.GetInt64(0) ?? 0L);
	}

	private void UpdateMessageStatistics(long totalMessages) {
		Statistics.TotalMessages = totalMessages;
	}

	private async Task<long> ComputeAttachmentStatistics() {
		using var conn = pool.Take();
		return await conn.ExecuteReaderAsync("SELECT COUNT(DISTINCT normalized_url) FROM attachments", static reader => reader?.GetInt64(0) ?? 0L);
	}

	private void UpdateAttachmentStatistics(long totalAttachments) {
		Statistics.TotalAttachments = totalAttachments;
	}

	private async Task<long> ComputeDownloadStatistics() {
		using var conn = pool.Take();
		return await conn.ExecuteReaderAsync("SELECT COUNT(*) FROM downloads", static reader => reader?.GetInt64(0) ?? 0L);
	}

	private void UpdateDownloadStatistics(long totalDownloads) {
		Statistics.TotalDownloads = totalDownloads;
	}
}
