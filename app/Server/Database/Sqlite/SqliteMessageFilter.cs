using System;
using System.Collections.Generic;
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

			if (filter.ChannelIds != null) {
				conditions.Add("channel_id IN (" + string.Join(",", filter.ChannelIds) + ")");
			}

			if (filter.UserIds != null) {
				conditions.Add("sender_id IN (" + string.Join(",", filter.UserIds) + ")");
			}

			if (filter.MessageIds != null) {
				conditions.Add("message_id IN (" + string.Join(",", filter.MessageIds) + ")");
			}

			return conditions.Count == 0 ? "" : " WHERE " + string.Join(" AND ", conditions);
		}
	}
}
