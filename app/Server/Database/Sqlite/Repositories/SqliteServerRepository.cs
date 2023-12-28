using System.Collections.Generic;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Utils;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Repositories;

sealed class SqliteServerRepository : IServerRepository {
	private readonly SqliteConnectionPool pool;
	private readonly DatabaseStatistics statistics;

	public SqliteServerRepository(SqliteConnectionPool pool, DatabaseStatistics statistics) {
		this.pool = pool;
		this.statistics = statistics;
	}

	internal async Task Initialize() {
		using var conn = pool.Take();
		await UpdateServerStatistics(conn);
	}

	private async Task UpdateServerStatistics(ISqliteConnection conn) {
		statistics.TotalServers = await conn.ExecuteReaderAsync("SELECT COUNT(*) FROM servers", static reader => reader?.GetInt64(0) ?? 0L);
	}

	public async Task Add(IReadOnlyList<Data.Server> servers) {
		using var conn = pool.Take();

		await using (var tx = await conn.BeginTransactionAsync()) {
			await using var cmd = conn.Upsert("servers", [
				("id", SqliteType.Integer),
				("name", SqliteType.Text),
				("type", SqliteType.Text)
			]);

			foreach (var server in servers) {
				cmd.Set(":id", server.Id);
				cmd.Set(":name", server.Name);
				cmd.Set(":type", ServerTypes.ToString(server.Type));
				await cmd.ExecuteNonQueryAsync();
			}

			await tx.CommitAsync();
		}

		await UpdateServerStatistics(conn);
	}

	public async IAsyncEnumerable<Data.Server> Get() {
		using var conn = pool.Take();

		await using var cmd = conn.Command("SELECT id, name, type FROM servers");
		await using var reader = await cmd.ExecuteReaderAsync();

		while (reader.Read()) {
			yield return new Data.Server {
				Id = reader.GetUint64(0),
				Name = reader.GetString(1),
				Type = ServerTypes.FromString(reader.GetString(2)),
			};
		}
	}
}
