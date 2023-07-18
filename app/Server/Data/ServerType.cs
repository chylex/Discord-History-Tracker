namespace DHT.Server.Data;

public enum ServerType {
	Server,
	Group,
	DirectMessage
}

public static class ServerTypes {
	public static ServerType? FromString(string? str) {
		return str switch {
			"SERVER" => ServerType.Server,
			"GROUP"  => ServerType.Group,
			"DM"     => ServerType.DirectMessage,
			_        => null
		};
	}

	public static string ToString(ServerType? type) {
		return type switch {
			ServerType.Server        => "SERVER",
			ServerType.Group         => "GROUP",
			ServerType.DirectMessage => "DM",
			_                        => "UNKNOWN"
		};
	}

	public static string ToNiceString(ServerType? type) {
		return type switch {
			ServerType.Server        => "Server",
			ServerType.Group         => "Group",
			ServerType.DirectMessage => "DM",
			_                        => "Unknown"
		};
	}

	internal static string ToJsonViewerString(ServerType? type) {
		return type switch {
			ServerType.Server        => "server",
			ServerType.Group         => "group",
			ServerType.DirectMessage => "user",
			_                        => "unknown"
		};
	}
}
