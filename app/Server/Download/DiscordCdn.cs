using System;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Web;

namespace DHT.Server.Download;

static class DiscordCdn {
	private static FrozenSet<string> CdnHosts { get; } = new[] {
		"cdn.discordapp.com",
		"cdn.discord.com",
		"media.discordapp.net"
	}.ToFrozenSet();

	private static bool IsCdnUrl(string originalUrl, [NotNullWhen(true)] out Uri? uri) {
		return Uri.TryCreate(originalUrl, UriKind.Absolute, out uri) && CdnHosts.Contains(uri.Host);
	}

	public static string NormalizeUrl(string originalUrl) {
		return IsCdnUrl(originalUrl, out var uri) ? DoNormalize(uri) : originalUrl;
	}

	public static bool NormalizeUrlAndReturnIfCdn(string originalUrl, out string normalizedUrl) {
		if (IsCdnUrl(originalUrl, out var uri)) {
			normalizedUrl = DoNormalize(uri);
			return true;
		}
		else {
			normalizedUrl = originalUrl;
			return false;
		}
	}

	private static string DoNormalize(Uri uri) {
		var query = HttpUtility.ParseQueryString(uri.Query);
		
		query.Remove("ex");
		query.Remove("is");
		query.Remove("hm");

		return new UriBuilder(uri) { Query = query.ToString() }.Uri.ToString();
	}
}
