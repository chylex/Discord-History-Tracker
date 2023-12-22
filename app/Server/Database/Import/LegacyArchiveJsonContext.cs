using System.Text.Json;
using System.Text.Json.Serialization;

namespace DHT.Server.Database.Import;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(JsonElement))]
sealed partial class LegacyArchiveJsonContext : JsonSerializerContext {}
