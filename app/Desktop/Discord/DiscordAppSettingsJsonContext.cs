using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DHT.Desktop.Discord;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = true)]
[JsonSerializable(typeof(JsonObject))]
sealed partial class DiscordAppSettingsJsonContext : JsonSerializerContext;
