using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DHT.Server.Endpoints.Responses;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Dictionary<ulong, string>))]
sealed partial class GetMessagesJsonContext : JsonSerializerContext {}
