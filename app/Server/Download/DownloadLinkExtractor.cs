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

	public static Data.Download FromUserAvatar(ulong userId, string avatarPath) {
		string url = $"https://cdn.discordapp.com/avatars/{userId}/{avatarPath}.webp";
		return new Data.Download(url, url, DownloadStatus.Pending, MediaTypeNames.Image.Webp, size: null);
	}

	public static Data.Download FromEmoji(ulong emojiId, EmojiFlags flags) {
		var isAnimated = flags.HasFlag(EmojiFlags.Animated);
		
		string ext = isAnimated ? "gif" : "webp";
		string type = isAnimated ? MediaTypeNames.Image.Gif : MediaTypeNames.Image.Webp;
		
		string url = $"https://cdn.discordapp.com/emojis/{emojiId}.{ext}";
		return new Data.Download(url, url, DownloadStatus.Pending, type, size: null);
	}

	public static Data.Download FromAttachment(Attachment attachment) {
		return new Data.Download(attachment.NormalizedUrl, attachment.DownloadUrl, DownloadStatus.Pending, attachment.Type, attachment.Size);
	}

	public static async Task<Data.Download?> TryFromEmbedJson(Stream jsonStream) {
		try {
			return FromEmbed(await JsonSerializer.DeserializeAsync(jsonStream, DiscordEmbedJsonContext.Default.DiscordEmbedJson));
		} catch (Exception e) {
			Log.Error("Could not parse embed json: " + e);
			return null;
		}
	}

	public static Data.Download? TryFromEmbedJson(string json) {
		try {
			return FromEmbed(JsonSerializer.Deserialize(json, DiscordEmbedJsonContext.Default.DiscordEmbedJson));
		} catch (Exception e) {
			Log.Error("Could not parse embed json: " + e);
			return null;
		}
	}
	
	private static Data.Download? FromEmbed(DiscordEmbedJson? embed) {
		if (embed is { Type: "image", Image.Url: {} imageUrl }) {
			return FromEmbedImage(imageUrl);
		}
		else if (embed is { Type: "video", Video.Url: {} videoUrl }) {
			return FromEmbedVideo(videoUrl);
		}
		else {
			return null;
		}
	}

	private static Data.Download? FromEmbedImage(string url) {
		if (DiscordCdn.NormalizeUrlAndReturnIfCdn(url, out var normalizedUrl)) {
			return new Data.Download(normalizedUrl, url, DownloadStatus.Pending, GuessImageType(normalizedUrl), size: null);
		}
		else {
			Log.Debug("Skipping non-CDN image url: " + url);
			return null;
		}
	}

	private static Data.Download? FromEmbedVideo(string url) {
		if (DiscordCdn.NormalizeUrlAndReturnIfCdn(url, out var normalizedUrl)) {
			return new Data.Download(normalizedUrl, url, DownloadStatus.Pending, GuessVideoType(normalizedUrl), size: null);
		}
		else {
			Log.Debug("Skipping non-CDN video url: " + url);
			return null;
		}
	}

	private static string? GuessImageType(string url) {
		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
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
			_       => null
		};
	}

	private static string? GuessVideoType(string url) {
		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
			return null;
		}

		string extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
		return extension switch {
			".mp4"  => "video/mp4",
			".mpeg" => "video/mpeg",
			".webm" => "video/webm",
			".mov"  => "video/quicktime",
			_       => null
		};
	}
}
