using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite.Repositories;

sealed class SqliteAttachmentRepository : BaseSqliteRepository, IAttachmentRepository {
	private readonly SqliteConnectionPool pool;

	public SqliteAttachmentRepository(SqliteConnectionPool pool) {
		this.pool = pool;
	}

	internal new void UpdateTotalCount() {
		base.UpdateTotalCount();
	}
	
	public override Task<long> Count(CancellationToken cancellationToken) {
		return Count(filter: null, cancellationToken);
	}

	public async Task<long> Count(AttachmentFilter? filter, CancellationToken cancellationToken) {
		await using var conn = await pool.Take();
		return await conn.ExecuteReaderAsync("SELECT COUNT(DISTINCT normalized_url) FROM attachments a" + filter.GenerateWhereClause("a"), static reader => reader?.GetInt64(0) ?? 0L, cancellationToken);
	}
}
