using DHT.Server.Service;

namespace DHT.Desktop.Server;

static class ServerConfiguration {
	public static ushort Port { get; set; } = ServerUtils.FindAvailablePort(min: 50000, max: 60000);
	public static string Token { get; set; } = ServerUtils.GenerateRandomToken(20);
}
