using System;
using System.Collections.Generic;
using DHT.Server.Data.Filters;

namespace DHT.Server.Database.Sqlite {
	static class SqliteMessageFilter {
		public static string GenerateWhereClause(this MessageFilter? filter, string? tableAlias = null, bool invert = false) {
			if (filter == null) {
				return "";
			}

			if (tableAlias != null) {
				tableAlias += ".";
			}

			List<string> conditions = new();

			if (filter.StartDate != null) {
				conditions.Add(tableAlias + "timestamp >= " + new DateTimeOffset(filter.StartDate.Value).ToUnixTimeMilliseconds());
			}

			if (filter.EndDate != null) {
				conditions.Add(tableAlias + "timestamp <= " + new DateTimeOffset(filter.EndDate.Value).ToUnixTimeMilliseconds());
			}

			if (filter.ChannelIds != null) {
				conditions.Add(tableAlias + "channel_id IN (" + string.Join(",", filter.ChannelIds) + ")");
			}

			if (filter.UserIds != null) {
				conditions.Add(tableAlias + "sender_id IN (" + string.Join(",", filter.UserIds) + ")");
			}

			if (filter.MessageIds != null) {
				conditions.Add(tableAlias + "message_id IN (" + string.Join(",", filter.MessageIds) + ")");
			}

			if (conditions.Count == 0) {
				return "";
			}

			if (invert) {
				return " WHERE NOT (" + string.Join(" AND ", conditions) + ")";
			}
			else {
				return " WHERE " + string.Join(" AND ", conditions);
			}
		}
	}
}
