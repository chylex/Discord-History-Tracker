using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Utils;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Repositories;

sealed class SqliteUserRepository : BaseSqliteRepository, IUserRepository {
	private readonly SqliteConnectionPool pool;
	
	public SqliteUserRepository(SqliteConnectionPool pool) {
		this.pool = pool;
	}
	
	public async Task Add(IReadOnlyList<User> users) {
		using (var conn = pool.Take()) {
			await using var tx = await conn.BeginTransactionAsync();
			
			await using var cmd = conn.Upsert("users", [
				("id", SqliteType.Integer),
				("name", SqliteType.Text),
				("avatar_url", SqliteType.Text),
				("discriminator", SqliteType.Text)
			]);

			foreach (var user in users) {
				cmd.Set(":id", user.Id);
				cmd.Set(":name", user.Name);
				cmd.Set(":avatar_url", user.AvatarUrl);
				cmd.Set(":discriminator", user.Discriminator);
				await cmd.ExecuteNonQueryAsync();
			}

			await tx.CommitAsync();
		}

		UpdateTotalCount();
	}

	public override async Task<long> Count(CancellationToken cancellationToken) {
		using var conn = pool.Take();
		return await conn.ExecuteReaderAsync("SELECT COUNT(*) FROM users", static reader => reader?.GetInt64(0) ?? 0L, cancellationToken);
	}

	public async IAsyncEnumerable<User> Get() {
		using var conn = pool.Take();
		
		await using var cmd = conn.Command("SELECT id, name, avatar_url, discriminator FROM users");
		await using var reader = await cmd.ExecuteReaderAsync();

		while (reader.Read()) {
			yield return new User {
				Id = reader.GetUint64(0),
				Name = reader.GetString(1),
				AvatarUrl = reader.IsDBNull(2) ? null : reader.GetString(2),
				Discriminator = reader.IsDBNull(3) ? null : reader.GetString(3),
			};
		}
	}
}
