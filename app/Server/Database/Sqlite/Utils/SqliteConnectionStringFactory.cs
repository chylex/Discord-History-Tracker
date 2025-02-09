using Microsoft.Data.Sqlite;

namespace DHT.Server.Database.Sqlite.Utils;

sealed record SqliteConnectionStringFactory(string Path) {
	public string Create() {
		var builder = new SqliteConnectionStringBuilder {
			DataSource = Path,
			Mode = SqliteOpenMode.ReadWriteCreate,
			Pooling = false,
		};
		
		return builder.ToString();
	}
}
