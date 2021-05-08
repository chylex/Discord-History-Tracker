using System;
using DHT.Server.Database.Sqlite;

namespace DHT.Server.Database.Exceptions {
	public class DatabaseTooNewException : Exception {
		public int DatabaseVersion { get; }
		public int CurrentVersion => Schema.Version;

		public DatabaseTooNewException(int databaseVersion) : base("Database is too new: " + databaseVersion + " > " + Schema.Version) {
			this.DatabaseVersion = databaseVersion;
		}
	}
}
