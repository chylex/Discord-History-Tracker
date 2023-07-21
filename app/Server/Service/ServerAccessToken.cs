using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DHT.Server.Service;

public sealed partial class ServerAccessToken {
	private readonly byte[] token;

	internal ServerAccessToken(string token) {
		this.token = Encoding.UTF8.GetBytes(token);
	}

	internal bool IsValid(string providedToken) {
		return CryptographicOperations.FixedTimeEquals(this.token, Encoding.UTF8.GetBytes(providedToken));
	}

	public static partial class Random {
		[GeneratedRegex("[^25679bcdfghjkmnpqrstwxyz]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		private static partial Regex AllowedCharactersRegex();

		public static string Generate(int length) {
			byte[] bytes = new byte[length * 3 / 2]; // Extra bytes compensate for filtered out characters.
			var rng = RandomNumberGenerator.Create();

			string token = "";
			while (token.Length < length) {
				rng.GetBytes(bytes);
				token = AllowedCharactersRegex().Replace(Convert.ToBase64String(bytes), "");
			}

			return token[..length];
		}
	}
}
