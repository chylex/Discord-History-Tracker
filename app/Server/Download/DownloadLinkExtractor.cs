using System;
using System.IO;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Embeds;
using DHT.Utils.Logging;

namespace DHT.Server.Download;

static class DownloadLinkExtractor {
	private static readonly Log Log = Log.ForType(typeof(DownloadLinkExtractor));
	
	public static FileUrl UserAvatar(ulong id, string avatarHash) {
		return new FileUrl($"https://cdn.discordapp.com/avatars/{id}/{avatarHash}.webp", MediaTypeNames.Image.Webp);
	}
	
	public static FileUrl Emoji(ulong emojiId, EmojiFlags flags) {
		bool isAnimated = flags.HasFlag(EmojiFlags.Animated);
		
		string ext = isAnimated ? "gif" : "webp";
		string type = isAnimated ? MediaTypeNames.Image.Gif : MediaTypeNames.Image.Webp;
		
		return new FileUrl($"https://cdn.discordapp.com/emojis/{emojiId}.{ext}", type);
	}
	
	public static async Task<FileUrl?> TryFromEmbedJson(Stream jsonStream) {
		try {
			return FromEmbed(await JsonSerializer.DeserializeAsync(jsonStream, DiscordEmbedJsonContext.Default.DiscordEmbedJson));
		} catch (Exception e) {
			Log.Error("Could not parse embed json: " + e);
			return null;
		}
	}
	
	public static FileUrl? TryFromEmbedJson(string json) {
		try {
			return FromEmbed(JsonSerializer.Deserialize(json, DiscordEmbedJsonContext.Default.DiscordEmbedJson));
		} catch (Exception e) {
			Log.Error("Could not parse embed json: " + e);
			return null;
		}
	}
	
	private static FileUrl? FromEmbed(DiscordEmbedJson? embed) {
		return embed switch {
			{ Type: "image", Image.Url: {} imageUrl } => FromEmbedImage(imageUrl),
			{ Type: "video", Video.Url: {} videoUrl } => FromEmbedVideo(videoUrl),
			_                                         => null,
		};
	}
	
	private static FileUrl? FromEmbedImage(string url) {
		if (DiscordCdn.NormalizeUrlAndReturnIfCdn(url, out string normalizedUrl)) {
			return new FileUrl(normalizedUrl, url, GuessImageType(normalizedUrl));
		}
		else {
			Log.Debug("Skipping non-CDN image url: " + url);
			return null;
		}
	}
	
	private static FileUrl? FromEmbedVideo(string url) {
		if (DiscordCdn.NormalizeUrlAndReturnIfCdn(url, out string normalizedUrl)) {
			return new FileUrl(normalizedUrl, url, GuessVideoType(normalizedUrl));
		}
		else {
			Log.Debug("Skipping non-CDN video url: " + url);
			return null;
		}
	}
	
	private static string? GuessImageType(string url) {
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) {
			return null;
		}
		
		ReadOnlySpan<char> extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
		
		// Remove Twitter quality suffix.
		int colonIndex = extension.IndexOf(':');
		if (colonIndex != -1) {
			extension = extension[..colonIndex];
		}
		
		return extension switch {
			".jpg"  => MediaTypeNames.Image.Jpeg,
			".jpeg" => MediaTypeNames.Image.Jpeg,
			".png"  => MediaTypeNames.Image.Png,
			".gif"  => MediaTypeNames.Image.Gif,
			".webp" => MediaTypeNames.Image.Webp,
			".bmp"  => MediaTypeNames.Image.Bmp,
			_       => null,
		};
	}
	
	private static string? GuessVideoType(string url) {
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) {
			return null;
		}
		
		string extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
		return extension switch {
			".mp4"  => "video/mp4",
			".mpeg" => "video/mpeg",
			".webm" => "video/webm",
			".mov"  => "video/quicktime",
			_       => null,
		};
	}
}
