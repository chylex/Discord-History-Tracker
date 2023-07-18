using System;
using System.Collections.Generic;
using System.Linq;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite;

static class SqliteFilters {
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

	public static string GenerateWhereClause(this AttachmentFilter? filter, string? tableAlias = null, bool invert = false) {
		if (filter == null) {
			return "";
		}

		var where = new SqliteWhereGenerator(tableAlias, invert);

		if (filter.MaxBytes != null) {
			where.AddCondition("size <= " + filter.MaxBytes);
		}

		if (filter.DownloadItemRule == AttachmentFilter.DownloadItemRules.OnlyNotPresent) {
			where.AddCondition("url NOT IN (SELECT url FROM downloads)");
		}
		else if (filter.DownloadItemRule == AttachmentFilter.DownloadItemRules.OnlyPresent) {
			where.AddCondition("url IN (SELECT url FROM downloads)");
		}

		return where.Generate();
	}

	public static string GenerateWhereClause(this DownloadItemFilter? filter, string? tableAlias = null, bool invert = false) {
		if (filter == null) {
			return "";
		}

		var where = new SqliteWhereGenerator(tableAlias, invert);

		if (filter.IncludeStatuses != null) {
			where.AddCondition("status IN (" + filter.IncludeStatuses.In() + ")");
		}

		if (filter.ExcludeStatuses != null) {
			where.AddCondition("status NOT IN (" + filter.ExcludeStatuses.In() + ")");
		}

		return where.Generate();
	}

	private static string In(this ISet<DownloadStatus> statuses) {
		return string.Join(",", statuses.Select(static status => (int) status));
	}
}
