using System.Collections.Generic;
using System.Threading.Tasks;
using DHT.Server.Data.Settings;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Repositories;
using DHT.Server.Database.Sqlite.Schema;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Utils.Logging;

namespace DHT.Server.Database.Sqlite;

public sealed class SqliteDatabaseFile : IDatabaseFile {
	private static readonly Log Log = Log.ForType<SqliteDatabaseFile>();
	
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
				yield return new AttachedDatabase(path + "_downloads", SqliteDownloadRepository.Schema);
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
		servers = new SqliteServerRepository(pool, downloads);
		channels = new SqliteChannelRepository(pool);
		messages = new SqliteMessageRepository(pool, downloads);
	}
	
	public async ValueTask DisposeAsync() {
		messages.Dispose();
		channels.Dispose();
		servers.Dispose();
		users.Dispose();
		downloads.Dispose();
		await pool.DisposeAsync();
	}
	
	public async Task Vacuum() {
		await using var conn = await pool.Take();
		
		Perf perf = Log.Start();
		await conn.ExecuteAsync("VACUUM");
		perf.Step("Vacuum main schema");
		
		await VacuumAttachedDatabase(conn, perf, SqliteDownloadRepository.Schema);
		
		perf.End();
		return;
		
		static async Task VacuumAttachedDatabase(ISqliteConnection conn, Perf perf, string schema) {
			if (conn.HasAttachedDatabase(schema)) {
				await conn.ExecuteAsync("VACUUM " + schema);
				perf.Step("Vacuum " + schema + " schema");
			}
		}
	}
}
