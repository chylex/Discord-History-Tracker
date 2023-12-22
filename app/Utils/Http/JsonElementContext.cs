using System.Text.Json;
using System.Text.Json.Serialization;

namespace DHT.Utils.Http;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(JsonElement))]
public sealed partial class JsonElementContext : JsonSerializerContext {}
