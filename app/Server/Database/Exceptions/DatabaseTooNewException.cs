using System;
using DHT.Server.Database.Sqlite;

namespace DHT.Server.Database.Exceptions;

public sealed class DatabaseTooNewException : Exception {
	public int DatabaseVersion { get; }
	public int CurrentVersion => SqliteSchema.Version;
	
	internal DatabaseTooNewException(int databaseVersion) : base("Database is too new: " + databaseVersion + " > " + SqliteSchema.Version) {
		this.DatabaseVersion = databaseVersion;
	}
}
