using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

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

	[GeneratedRegex("[^25679bcdfghjkmnpqrstwxyz]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex TokenFilterRegex();
	
	private static Regex TokenFilter { get; } = TokenFilterRegex();

	public static string GenerateRandomToken(int length) {
		byte[] bytes = new byte[length * 3 / 2]; // Extra bytes compensate for filtered out characters.
		var rng = RandomNumberGenerator.Create();

		string token = "";
		while (token.Length < length) {
			rng.GetBytes(bytes);
			token = TokenFilter.Replace(Convert.ToBase64String(bytes), "");
		}

		return token[..length];
	}
}
