using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Utils;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Repositories;

sealed class SqliteChannelRepository : BaseSqliteRepository, IChannelRepository {
	private readonly SqliteConnectionPool pool;

	public SqliteChannelRepository(SqliteConnectionPool pool) {
		this.pool = pool;
	}

	public async Task Add(IReadOnlyList<Channel> channels) {
		await using var conn = await pool.Take();

		await using (var tx = await conn.BeginTransactionAsync()) {
			await using var cmd = conn.Upsert("channels", [
				("id", SqliteType.Integer),
				("server", SqliteType.Integer),
				("name", SqliteType.Text),
				("parent_id", SqliteType.Integer),
				("position", SqliteType.Integer),
				("topic", SqliteType.Text),
				("nsfw", SqliteType.Integer)
			]);

			foreach (var channel in channels) {
				cmd.Set(":id", channel.Id);
				cmd.Set(":server", channel.Server);
				cmd.Set(":name", channel.Name);
				cmd.Set(":parent_id", channel.ParentId);
				cmd.Set(":position", channel.Position);
				cmd.Set(":topic", channel.Topic);
				cmd.Set(":nsfw", channel.Nsfw);
				await cmd.ExecuteNonQueryAsync();
			}

			await tx.CommitAsync();
		}

		UpdateTotalCount();
	}

	public override async Task<long> Count(CancellationToken cancellationToken) {
		await using var conn = await pool.Take();
		return await conn.ExecuteReaderAsync("SELECT COUNT(*) FROM channels", static reader => reader?.GetInt64(0) ?? 0L, cancellationToken);
	}

	public async IAsyncEnumerable<Channel> Get() {
		await using var conn = await pool.Take();

		await using var cmd = conn.Command("SELECT id, server, name, parent_id, position, topic, nsfw FROM channels");
		await using var reader = await cmd.ExecuteReaderAsync();

		while (reader.Read()) {
			yield return new Channel {
				Id = reader.GetUint64(0),
				Server = reader.GetUint64(1),
				Name = reader.GetString(2),
				ParentId = reader.IsDBNull(3) ? null : reader.GetUint64(3),
				Position = reader.IsDBNull(4) ? null : reader.GetInt32(4),
				Topic = reader.IsDBNull(5) ? null : reader.GetString(5),
				Nsfw = reader.IsDBNull(6) ? null : reader.GetBoolean(6),
			};
		}
	}
}
