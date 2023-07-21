using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace DHT.Server.Service;

public static partial class ServerUtils {
	public static ushort FindAvailablePort(ushort min, ushort max) {
		var properties = IPGlobalProperties.GetIPGlobalProperties();
		var occupied = new HashSet<int>();
		occupied.UnionWith(properties.GetActiveTcpListeners().Select(static tcp => tcp.Port));
		occupied.UnionWith(properties.GetActiveTcpConnections().Select(static tcp => tcp.LocalEndPoint.Port));

		for (int port = min; port < max; port++) {
			if (!occupied.Contains(port)) {
				return (ushort) port;
			}
		}

		return min;
	}

}
