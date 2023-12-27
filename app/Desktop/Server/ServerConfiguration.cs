using DHT.Server.Service;

namespace DHT.Desktop.Server;

static class ServerConfiguration {
	public static ushort Port { get; set; } = ServerUtils.FindAvailablePort(50000, 60000);
	public static string Token { get; set; } = ServerUtils.GenerateRandomToken(20);
}
