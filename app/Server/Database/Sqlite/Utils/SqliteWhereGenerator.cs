using System.Collections.Generic;

namespace DHT.Server.Database.Sqlite.Utils;

sealed class SqliteWhereGenerator {
	private readonly string? tableAlias;
	private readonly bool invert;
	private readonly List<string> conditions = new ();

	public SqliteWhereGenerator(string? tableAlias, bool invert) {
		this.tableAlias = tableAlias;
		this.invert = invert;
	}

	public void AddCondition(string condition) {
		conditions.Add(tableAlias == null ? condition : tableAlias + '.' + condition);
	}

	public string Generate() {
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
