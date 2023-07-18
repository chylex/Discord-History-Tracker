using System;

namespace DHT.Server.Database.Exceptions;

public sealed class InvalidDatabaseVersionException : Exception {
	public string Version { get; }

	internal InvalidDatabaseVersionException(string version) : base("Invalid database version: " + version) {
		this.Version = version;
	}
}
