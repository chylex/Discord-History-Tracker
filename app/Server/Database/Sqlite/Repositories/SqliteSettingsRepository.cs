using System;
using System.Threading.Tasks;
using DHT.Server.Data.Settings;
using DHT.Server.Database.Repositories;
using DHT.Server.Database.Sqlite.Utils;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Repositories;

sealed class SqliteSettingsRepository(SqliteConnectionPool pool) : ISettingsRepository {
	public Task Set<T>(SettingsKey<T> key, T value) {
		return Set(setter => setter.Set(key, value));
	}
	
	public async Task Set(Func<ISettingsRepository.ISetter, Task> setter) {
		await using var conn = await pool.Take();
		await conn.BeginTransactionAsync();
		
		await using var cmd = conn.Command(
			"""
			INSERT INTO metadata (key, value)
			VALUES (:key, :value)
			ON CONFLICT (key)
			DO UPDATE SET value = excluded.value
			"""
		);
		
		cmd.Add(":key", SqliteType.Text);
		cmd.Add(":value", SqliteType.Text);
		
		await setter(new Setter(cmd));
		
		await cmd.ExecuteNonQueryAsync();
		await conn.CommitTransactionAsync();
	}
	
	private sealed class Setter(SqliteCommand cmd) : ISettingsRepository.ISetter {
		public async Task Set<T>(SettingsKey<T> key, T value) {
			cmd.Set(":key", key.Key);
			cmd.Set(":value", key.ToString(value));
			await cmd.ExecuteNonQueryAsync();
		}
	}
	
	public async Task<T?> Get<T>(SettingsKey<T> key, T? defaultValue) {
		string? value;
		
		await using (var conn = await pool.Take()) {
			await using var cmd = conn.Command("SELECT value FROM metadata WHERE key = :key");
			cmd.AddAndSet(":key", SqliteType.Text, key.Key);
			
			await using var reader = await cmd.ExecuteReaderAsync();
			value = await reader.ReadAsync() ? reader.GetString(0) : null;
		}
		
		return value != null && key.FromString(value, out T convertedValue) ? convertedValue : defaultValue;
	}
}
