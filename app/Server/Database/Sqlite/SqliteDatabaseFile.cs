using System.Collections.Generic;
using System.Threading.Tasks;
using DHT.Server.Data.Settings;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Repositories;
using DHT.Server.Database.Sqlite.Schema;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite;

public sealed class SqliteDatabaseFile : IDatabaseFile {
	private const int DefaultPoolSize = 5;
	
	public static async Task<SqliteDatabaseFile?> OpenOrCreate(string path, ISchemaUpgradeCallbacks schemaUpgradeCallbacks) {
		var connectionString = new SqliteConnectionStringFactory(path);
		var attachedDatabaseCollector = new AttachedDatabaseCollector(path);
		
		bool wasOpened;
		await using (var conn = await CustomSqliteConnection.OpenUnpooled(connectionString)) {
			wasOpened = await new SqliteSchema(conn).Setup(attachedDatabaseCollector, schemaUpgradeCallbacks);
		}
		
		if (wasOpened) {
			var pool = await SqliteConnectionPool.Create(connectionString, DefaultPoolSize, attachedDatabaseCollector);
			return new SqliteDatabaseFile(path, pool);
		}
		else {
			return null;
		}
	}
	
	private sealed class AttachedDatabaseCollector(string path) : ISqliteAttachedDatabaseCollector {
		public async IAsyncEnumerable<AttachedDatabase> GetAttachedDatabases(ISqliteConnection conn) {
			bool useSeparateFileForDownloads = await SqliteSettingsRepository.Get(conn, SettingsKey.SeparateFileForDownloads, defaultValue: false);
			if (useSeparateFileForDownloads) {
				yield return new AttachedDatabase(path + "_dl", SqliteDownloadRepository.Schema);
			}
		}
	}
	
	public string Path { get; }
	
	public ISettingsRepository Settings => settings;
	public IUserRepository Users => users;
	public IServerRepository Servers => servers;
	public IChannelRepository Channels => channels;
	public IMessageRepository Messages => messages;
	public IDownloadRepository Downloads => downloads;
	
	private readonly SqliteConnectionPool pool;
	
	private readonly SqliteSettingsRepository settings;
	private readonly SqliteUserRepository users;
	private readonly SqliteServerRepository servers;
	private readonly SqliteChannelRepository channels;
	private readonly SqliteMessageRepository messages;
	private readonly SqliteDownloadRepository downloads;
	
	private SqliteDatabaseFile(string path, SqliteConnectionPool pool) {
		this.Path = path;
		this.pool = pool;
		
		downloads = new SqliteDownloadRepository(pool);
		settings = new SqliteSettingsRepository(pool);
		users = new SqliteUserRepository(pool, downloads);
		servers = new SqliteServerRepository(pool);
		channels = new SqliteChannelRepository(pool);
		messages = new SqliteMessageRepository(pool, downloads);
	}
	
	public async ValueTask DisposeAsync() {
		users.Dispose();
		servers.Dispose();
		channels.Dispose();
		messages.Dispose();
		downloads.Dispose();
		await pool.DisposeAsync();
	}
	
	public async Task Vacuum() {
		await using var conn = await pool.Take();
		
		await conn.ExecuteAsync("VACUUM");
		
		if (await conn.HasAttachedDatabase(SqliteDownloadRepository.Schema)) {
			await conn.ExecuteAsync("VACUUM " + SqliteDownloadRepository.Schema);
		}
	}
}
