using System;
using System.Collections.Generic;
using System.Linq;
using DHT.Server.Data.Filters;

namespace DHT.Server.Database.Sqlite {
	public static class SqliteMessageFilter {
		public static string GenerateWhereClause(this MessageFilter? filter) {
			if (filter == null) {
				return "";
			}

			List<string> conditions = new();

			if (filter.StartDate != null) {
				conditions.Add("timestamp >= " + new DateTimeOffset(filter.StartDate.Value).ToUnixTimeMilliseconds());
			}

			if (filter.EndDate != null) {
				conditions.Add("timestamp <= " + new DateTimeOffset(filter.EndDate.Value).ToUnixTimeMilliseconds());
			}

			if (filter.MessageIds.Count > 0) {
				conditions.Add("(" + string.Join(" OR ", filter.MessageIds.Select(id => "message_id = " + id)) + ")");
			}

			return conditions.Count == 0 ? "" : " WHERE " + string.Join(" AND ", conditions);
		}
	}
}
