using System;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite {
	static class SqliteMessageFilter {
		public static string GenerateWhereClause(this MessageFilter? filter, string? tableAlias = null, bool invert = false) {
			if (filter == null) {
				return "";
			}

			var where = new SqliteWhereGenerator(tableAlias, invert);

			if (filter.StartDate != null) {
				where.AddCondition("timestamp >= " + new DateTimeOffset(filter.StartDate.Value).ToUnixTimeMilliseconds());
			}

			if (filter.EndDate != null) {
				where.AddCondition("timestamp <= " + new DateTimeOffset(filter.EndDate.Value).ToUnixTimeMilliseconds());
			}

			if (filter.ChannelIds != null) {
				where.AddCondition("channel_id IN (" + string.Join(",", filter.ChannelIds) + ")");
			}

			if (filter.UserIds != null) {
				where.AddCondition("sender_id IN (" + string.Join(",", filter.UserIds) + ")");
			}

			if (filter.MessageIds != null) {
				where.AddCondition("message_id IN (" + string.Join(",", filter.MessageIds) + ")");
			}

			return where.Generate();
		}
	}
}
