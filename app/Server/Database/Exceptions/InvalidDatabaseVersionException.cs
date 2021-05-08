using System;

namespace DHT.Server.Database.Exceptions {
	public class InvalidDatabaseVersionException : Exception {
		public string Version { get; }

		public InvalidDatabaseVersionException(string version) : base("Invalid database version: " + version) {
			this.Version = version;
		}
	}
}
