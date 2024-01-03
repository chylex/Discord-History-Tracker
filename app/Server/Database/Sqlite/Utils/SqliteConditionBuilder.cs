using System.Collections.Generic;

namespace DHT.Server.Database.Sqlite.Utils;

sealed class SqliteConditionBuilder {
	private readonly string? tableAlias;
	private readonly bool invert;
	private readonly List<string> conditions = [];

	public SqliteConditionBuilder(string? tableAlias, bool invert) {
		this.tableAlias = tableAlias;
		this.invert = invert;
	}

	public void AddCondition(string condition) {
		conditions.Add(tableAlias == null ? condition : tableAlias + '.' + condition);
	}

	public string Build() {
		if (conditions.Count == 0) {
			return invert ? "FALSE" : "TRUE";
		}

		if (invert) {
			return "NOT (" + string.Join(" AND ", conditions) + ")";
		}
		else {
			return string.Join(" AND ", conditions);
		}
	}
	
	public string BuildWhereClause() {
		return " WHERE " + Build();
	}
}
