using System.Diagnostics.CodeAnalysis;

namespace DHT.Server.Database;

[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
public sealed class DummyDatabaseFile : IDatabaseFile {
	public static DummyDatabaseFile Instance { get; } = new();

	public string Path => "";

	private DummyDatabaseFile() {}

	public void Vacuum() {}

	public void Dispose() {}
}
