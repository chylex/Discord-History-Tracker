using System;
using System.Threading.Tasks;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Repositories;
using DHT.Server.Database.Sqlite.Schema;
using DHT.Server.Database.Sqlite.Utils;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite;

public sealed class SqliteDatabaseFile : IDatabaseFile {
	private const int DefaultPoolSize = 5;
	
	public static async Task<SqliteDatabaseFile?> OpenOrCreate(string path, ISchemaUpgradeCallbacks schemaUpgradeCallbacks) {
		var connectionString = new SqliteConnectionStringBuilder {
			DataSource = path,
			Mode = SqliteOpenMode.ReadWriteCreate,
		};
		
		var pool = await SqliteConnectionPool.Create(connectionString, DefaultPoolSize);
		bool wasOpened;
		
		try {
			await using var conn = await pool.Take();
			wasOpened = await new SqliteSchema(conn).Setup(schemaUpgradeCallbacks);
		} catch (Exception) {
			await pool.DisposeAsync();
			throw;
		}
		
		if (wasOpened) {
			return new SqliteDatabaseFile(path, pool);
		}
		else {
			await pool.DisposeAsync();
			return null;
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
	}
}
