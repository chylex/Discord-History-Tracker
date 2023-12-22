using System.Text.Json.Serialization;

namespace DHT.Server.Database.Import; 

sealed class DiscordEmbedLegacyJson {
	public required string Url { get; init; }
	public required string Type { get; init; }
	
	public bool DhtLegacy { get; } = true;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Title { get; init; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Description { get; init; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ImageJson? Image { get; init; }

	public sealed class ImageJson {
		public required string Url { get; init; }
	}
}
