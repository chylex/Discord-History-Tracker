using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Utils;
using DHT.Utils.Logging;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Repositories;

sealed class SqliteServerRepository(SqliteConnectionPool pool, SqliteDownloadRepository downloads) : BaseSqliteRepository(Log), IServerRepository {
	private static readonly Log Log = Log.ForType<SqliteServerRepository>();
	
	public async Task Add(IReadOnlyList<Data.Server> servers) {
		await using (var conn = await pool.Take()) {
			await conn.BeginTransactionAsync();
			
			await using var cmd = conn.Upsert("servers", [
				("id", SqliteType.Integer),
				("name", SqliteType.Text),
				("type", SqliteType.Text),
				("icon_hash", SqliteType.Text),
			]);
			
			await using var downloadCollector = new SqliteDownloadRepository.NewDownloadCollector(downloads, conn);
			
			foreach (Data.Server server in servers) {
				cmd.Set(":id", server.Id);
				cmd.Set(":name", server.Name);
				cmd.Set(":type", ServerTypes.ToString(server.Type));
				cmd.Set(":icon_hash", server.IconHash);
				await cmd.ExecuteNonQueryAsync();
				await downloadCollector.AddIfNotNull(server.IconUrl?.ToPendingDownload());
			}
			
			await conn.CommitTransactionAsync();
		}
		
		UpdateTotalCount();
	}
	
	public override async Task<long> Count(CancellationToken cancellationToken) {
		await using var conn = await pool.Take();
		return await conn.ExecuteReaderAsync("SELECT COUNT(*) FROM servers", static reader => reader?.GetInt64(0) ?? 0L, cancellationToken);
	}
	
	public async IAsyncEnumerable<Data.Server> Get([EnumeratorCancellation] CancellationToken cancellationToken) {
		await using var conn = await pool.Take();
		
		await using var cmd = conn.Command("SELECT id, name, type, icon_hash FROM servers");
		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		
		while (await reader.ReadAsync(cancellationToken)) {
			yield return new Data.Server {
				Id = reader.GetUint64(0),
				Name = reader.GetString(1),
				Type = ServerTypes.FromString(reader.GetString(2)),
				IconHash = reader.IsDBNull(3) ? null : reader.GetString(3),
			};
		}
	}
	
	public async Task<int> RemoveUnreachable() {
		int removed;
		await using (var conn = await pool.Take()) {
			removed = await conn.ExecuteAsync("DELETE FROM servers WHERE id NOT IN (SELECT DISTINCT server FROM channels)");
		}
		
		UpdateTotalCount();
		return removed;
	}
}
