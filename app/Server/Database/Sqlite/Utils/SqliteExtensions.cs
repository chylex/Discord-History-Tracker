using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Utils;

static class SqliteExtensions {
	public static SqliteCommand Command(this ISqliteConnection conn, [LanguageInjection("sql")] string sql) {
		var cmd = conn.InnerConnection.CreateCommand();
		cmd.CommandText = sql;
		return cmd;
	}
	
	public static async Task<int> ExecuteAsync(this ISqliteConnection conn, [LanguageInjection("sql")] string sql, CancellationToken cancellationToken = default) {
		await using var cmd = conn.Command(sql);
		return await cmd.ExecuteNonQueryAsync(cancellationToken);
	}
	
	public static async Task<T> ExecuteReaderAsync<T>(this ISqliteConnection conn, string sql, Func<SqliteDataReader?, T> readFunction, CancellationToken cancellationToken = default) {
		await using var cmd = conn.Command(sql);
		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		
		return await reader.ReadAsync(cancellationToken) ? readFunction(reader) : readFunction(null);
	}
	
	public static async Task<long> ExecuteLongScalarAsync(this SqliteCommand command) {
		return (long) (await command.ExecuteScalarAsync())!;
	}
	
	public static SqliteCommand Insert(this ISqliteConnection conn, [LanguageInjection("sql", Prefix = "SELECT * FROM ")] string tableName, (string Name, SqliteType Type)[] columns) {
		string columnNames = string.Join(separator: ',', columns.Select(static c => c.Name));
		string columnParams = string.Join(separator: ',', columns.Select(static c => ':' + c.Name));
		
		var cmd = conn.Command("INSERT INTO " + tableName + " (" + columnNames + ")" +
		                       "VALUES (" + columnParams + ")");
		
		CreateParameters(cmd, columns);
		return cmd;
	}
	
	public static SqliteCommand Upsert(this ISqliteConnection conn, [LanguageInjection("sql", Prefix = "SELECT * FROM ")] string tableName, (string Name, SqliteType Type)[] columns) {
		string columnNames = string.Join(separator: ',', columns.Select(static c => c.Name));
		string columnParams = string.Join(separator: ',', columns.Select(static c => ':' + c.Name));
		string columnUpdates = string.Join(separator: ',', columns.Skip(1).Select(static c => c.Name + " = excluded." + c.Name));
		
		var cmd = conn.Command("INSERT INTO " + tableName + " (" + columnNames + ")" +
		                       "VALUES (" + columnParams + ")" +
		                       "ON CONFLICT (" + columns[0].Name + ")" +
		                       "DO UPDATE SET " + columnUpdates);
		
		CreateParameters(cmd, columns);
		return cmd;
	}
	
	public static SqliteCommand Delete(this ISqliteConnection conn, [LanguageInjection("sql", Prefix = "SELECT * FROM ")] string tableName, (string Name, SqliteType Type) column) {
		var cmd = conn.Command("DELETE FROM " + tableName + " WHERE " + column.Name + " = :" + column.Name);
		CreateParameters(cmd, [column]);
		return cmd;
	}
	
	private static void CreateParameters(SqliteCommand cmd, (string Name, SqliteType Type)[] columns) {
		foreach ((string name, SqliteType type) in columns) {
			cmd.Parameters.Add(":" + name, type);
		}
	}
	
	public static void Add(this SqliteCommand cmd, string key, SqliteType type) {
		cmd.Parameters.Add(key, type);
	}
	
	public static void AddAndSet(this SqliteCommand cmd, string key, SqliteType type, object? value) {
		cmd.Parameters.Add(key, type).Value = value ?? DBNull.Value;
	}
	
	public static void Set(this SqliteCommand cmd, string key, object? value) {
		cmd.Parameters[key].Value = value ?? DBNull.Value;
	}
	
	public static ulong GetUint64(this SqliteDataReader reader, int ordinal) {
		return (ulong) reader.GetInt64(ordinal);
	}
}
