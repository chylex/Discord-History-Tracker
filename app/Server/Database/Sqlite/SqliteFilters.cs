using System;
using System.Collections.Generic;
using System.Linq;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite;

static class SqliteFilters {
	public static SqliteConditionBuilder GenerateConditions(this MessageFilter? filter, string? tableAlias = null, bool invert = false) {
		var builder = new SqliteConditionBuilder(tableAlias, invert);

		if (filter != null) {
			if (filter.StartDate != null) {
				builder.AddCondition("timestamp >= " + new DateTimeOffset(filter.StartDate.Value).ToUnixTimeMilliseconds());
			}

			if (filter.EndDate != null) {
				builder.AddCondition("timestamp <= " + new DateTimeOffset(filter.EndDate.Value).ToUnixTimeMilliseconds());
			}

			if (filter.ChannelIds != null) {
				builder.AddCondition("channel_id IN (" + string.Join(",", filter.ChannelIds) + ")");
			}

			if (filter.UserIds != null) {
				builder.AddCondition("sender_id IN (" + string.Join(",", filter.UserIds) + ")");
			}

			if (filter.MessageIds != null) {
				builder.AddCondition("message_id IN (" + string.Join(",", filter.MessageIds) + ")");
			}
		}

		return builder;
	}

	public static SqliteConditionBuilder GenerateConditions(this DownloadItemFilter? filter, string? tableAlias = null, bool invert = false) {
		var builder = new SqliteConditionBuilder(tableAlias, invert);

		if (filter != null) {
			if (filter.IncludeStatuses != null) {
				builder.AddCondition("status IN (" + filter.IncludeStatuses.In() + ")");
			}

			if (filter.ExcludeStatuses != null) {
				builder.AddCondition("status NOT IN (" + filter.ExcludeStatuses.In() + ")");
			}

			if (filter.MaxBytes != null) {
				builder.AddCondition("size IS NOT NULL");
				builder.AddCondition("size <= " + filter.MaxBytes);
			}
		}

		return builder;
	}

	private static string In(this ISet<DownloadStatus> statuses) {
		return string.Join(",", statuses.Select(static status => (int) status));
	}
}
