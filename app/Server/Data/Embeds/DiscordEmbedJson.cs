namespace DHT.Server.Data.Embeds;

sealed class DiscordEmbedJson {
	public string? Type { get; set; }
	public string? Url { get; set; }
	
	public JsonImage? Image { get; set; }
	public JsonImage? Thumbnail { get; set; }
	public JsonImage? Video { get; set; }
	
	public sealed class JsonImage {
		public string? Url { get; set; }
		public string? ProxyUrl { get; set; }
		public int? Width { get; set; }
		public int? Height { get; set; }
	}
}
