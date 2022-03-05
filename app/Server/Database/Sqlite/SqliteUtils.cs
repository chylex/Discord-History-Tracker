using System;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite {
	static class SqliteUtils {
		public static SqliteCommand Command(this SqliteConnection conn, string sql) {
			var cmd = conn.CreateCommand();
			cmd.CommandText = sql;
			return cmd;
		}

		public static object? SelectScalar(this SqliteConnection conn, string sql) {
			using var cmd = conn.Command(sql);
			return cmd.ExecuteScalar();
		}

		public static SqliteCommand Insert(this SqliteConnection conn, string tableName, (string Name, SqliteType Type)[] columns) {
			string columnNames = string.Join(',', columns.Select(static c => c.Name));
			string columnParams = string.Join(',', columns.Select(static c => ':' + c.Name));

			var cmd = conn.Command("INSERT INTO " + tableName + " (" + columnNames + ")" +
			                       "VALUES (" + columnParams + ")");

			CreateParameters(cmd, columns);
			return cmd;
		}

		public static SqliteCommand Upsert(this SqliteConnection conn, string tableName, (string Name, SqliteType Type)[] columns) {
			string columnNames = string.Join(',', columns.Select(static c => c.Name));
			string columnParams = string.Join(',', columns.Select(static c => ':' + c.Name));
			string columnUpdates = string.Join(',', columns.Skip(1).Select(static c => c.Name + " = excluded." + c.Name));

			var cmd = conn.Command("INSERT INTO " + tableName + " (" + columnNames + ")" +
			                       "VALUES (" + columnParams + ")" +
			                       "ON CONFLICT (" + columns[0].Name + ")" +
			                       "DO UPDATE SET " + columnUpdates);

			CreateParameters(cmd, columns);
			return cmd;
		}

		public static SqliteCommand Delete(this SqliteConnection conn, string tableName, (string Name, SqliteType Type) column) {
			var cmd = conn.Command("DELETE FROM " + tableName + " WHERE " + column.Name + " = :" + column.Name);
			CreateParameters(cmd, new [] { column });
			return cmd;
		}

		private static void CreateParameters(SqliteCommand cmd, (string Name, SqliteType Type)[] columns) {
			foreach (var (name, type) in columns) {
				cmd.Parameters.Add(":" + name, type);
			}
		}

		public static void Set(this SqliteCommand cmd, string key, object? value) {
			cmd.Parameters[key].Value = value ?? DBNull.Value;
		}

		public static ulong GetUint64(this SqliteDataReader reader, int ordinal) {
			return (ulong) reader.GetInt64(ordinal);
		}
	}
}
