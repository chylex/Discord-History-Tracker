using System;

namespace DHT.Server.Database;

public interface IDatabaseFile : IDisposable {
	string Path { get; }


	void Vacuum();
}
