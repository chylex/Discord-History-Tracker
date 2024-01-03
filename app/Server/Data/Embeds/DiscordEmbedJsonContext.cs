using System.Text.Json.Serialization;

namespace DHT.Server.Data.Embeds;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(DiscordEmbedJson))]
sealed partial class DiscordEmbedJsonContext : JsonSerializerContext;
