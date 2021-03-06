using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace DHT.Server.Service {
	public static class ServerUtils {
		public static int FindAvailablePort(int min, int max) {
			var properties = IPGlobalProperties.GetIPGlobalProperties();
			var occupied = new HashSet<int>();
			occupied.UnionWith(properties.GetActiveTcpListeners().Select(tcp => tcp.Port));
			occupied.UnionWith(properties.GetActiveTcpConnections().Select(tcp => tcp.LocalEndPoint.Port));

			for (int port = min; port < max; port++) {
				if (!occupied.Contains(port)) {
					return port;
				}
			}

			return min;
		}

		private static Regex TokenFilter { get; } = new("[^25679bcdfghjkmnpqrstwxyz]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		public static string GenerateRandomToken(int length) {
			byte[] bytes = new byte[length * 3 / 2]; // Extra bytes compensate for filtered out characters.
			var rng = new RNGCryptoServiceProvider();

			string token = "";
			while (token.Length < length) {
				rng.GetBytes(bytes);
				token = TokenFilter.Replace(Convert.ToBase64String(bytes), "");
			}

			return token[..length];
		}
	}
}
