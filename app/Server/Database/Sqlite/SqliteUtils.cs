using System;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite {
	public static class SqliteUtils {
		public static SqliteCommand Command(this SqliteConnection conn, string sql) {
			var cmd = conn.CreateCommand();
			cmd.CommandText = sql;
			return cmd;
		}

		public static SqliteCommand Insert(this SqliteConnection conn, string tableName, string[] columns) {
			string columnNames = string.Join(',', columns);
			string columnParams = string.Join(',', columns.Select(c => ':' + c));

			return conn.Command("INSERT INTO " + tableName + " (" + columnNames + ")" +
			                    "VALUES (" + columnParams + ")");
		}

		public static SqliteCommand Upsert(this SqliteConnection conn, string tableName, string[] columns) {
			string columnNames = string.Join(',', columns);
			string columnParams = string.Join(',', columns.Select(c => ':' + c));
			string columnUpdates = string.Join(',', columns.Skip(1).Select(c => c + " = excluded." + c));

			return conn.Command("INSERT INTO " + tableName + " (" + columnNames + ")" +
			                    "VALUES (" + columnParams + ")" +
			                    "ON CONFLICT (" + columns[0] + ")" +
			                    "DO UPDATE SET " + columnUpdates);
		}

		public static void AddAndSet(this SqliteParameterCollection parameters, string key, object? value) {
			parameters.AddWithValue(key, value ?? DBNull.Value);
		}

		public static void Set(this SqliteParameterCollection parameters, string key, object? value) {
			parameters[key].Value = value ?? DBNull.Value;
		}
	}
}
