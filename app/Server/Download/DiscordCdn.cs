using System;
using System.Collections.Frozen;

namespace DHT.Server.Download;

static class DiscordCdn {
	private static FrozenSet<string> CdnHosts { get; } = new[] {
		"cdn.discordapp.com",
		"cdn.discord.com",
	}.ToFrozenSet();

	public static string NormalizeUrl(string originalUrl) {
		return Uri.TryCreate(originalUrl, UriKind.Absolute, out var uri) && CdnHosts.Contains(uri.Host) ? uri.GetLeftPart(UriPartial.Path) : originalUrl;
	}
}
