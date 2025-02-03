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

sealed class SqliteServerRepository : BaseSqliteRepository, IServerRepository {
	private static readonly Log Log = Log.ForType<SqliteServerRepository>();
	
	private readonly SqliteConnectionPool pool;
	
	public SqliteServerRepository(SqliteConnectionPool pool) : base(Log) {
		this.pool = pool;
	}
	
	public async Task Add(IReadOnlyList<Data.Server> servers) {
		await using (var conn = await pool.Take()) {
			await conn.BeginTransactionAsync();
			
			await using var cmd = conn.Upsert("servers", [
				("id", SqliteType.Integer),
				("name", SqliteType.Text),
				("type", SqliteType.Text),
			]);
			
			foreach (Data.Server server in servers) {
				cmd.Set(":id", server.Id);
				cmd.Set(":name", server.Name);
				cmd.Set(":type", ServerTypes.ToString(server.Type));
				await cmd.ExecuteNonQueryAsync();
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
		
		await using var cmd = conn.Command("SELECT id, name, type FROM servers");
		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		
		while (await reader.ReadAsync(cancellationToken)) {
			yield return new Data.Server {
				Id = reader.GetUint64(0),
				Name = reader.GetString(1),
				Type = ServerTypes.FromString(reader.GetString(2)),
			};
		}
	}
}
