using System;
using System.Threading.Tasks;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Utils.Logging;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite;

public sealed class SqliteDatabaseFile : IDatabaseFile {
	private const int DefaultPoolSize = 5;

	private static readonly string[] InitialCommands = {
		"PRAGMA journal_mode=WAL",
		"PRAGMA foreign_keys=1",
	};

	public static async Task<SqliteDatabaseFile?> OpenOrCreate(string path, Func<Task<bool>> checkCanUpgradeSchemas) {
		var connectionString = new SqliteConnectionStringBuilder {
			Mode = SqliteOpenMode.ReadWriteCreate,
			DataSource = path,
		};

		var pool = new SqliteConnectionPool(connectionString, DefaultPoolSize, InitialCommands);

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

	private readonly Log log;
	private readonly SqliteConnectionPool pool;

	private SqliteDatabaseFile(string path, SqliteConnectionPool pool) {
		this.Path = path;
		this.log = Log.ForType(typeof(SqliteDatabaseFile), System.IO.Path.GetFileName(path));
		this.pool = pool;
	}

	public void Dispose() {
		pool.Dispose();
	}

	public void Vacuum() {
		using var conn = pool.Take();
		using var cmd = conn.Command("VACUUM");
		cmd.ExecuteNonQuery();
	}
}
