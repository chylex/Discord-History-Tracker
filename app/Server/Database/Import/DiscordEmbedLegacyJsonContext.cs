using System.Text.Json.Serialization;

namespace DHT.Server.Database.Import;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(DiscordEmbedLegacyJson))]
sealed partial class DiscordEmbedLegacyJsonContext : JsonSerializerContext;
