using System.Text.Json.Serialization;

namespace DHT.Server.Database.Export;

[JsonSourceGenerationOptions(
	Converters = [typeof(SnowflakeJsonSerializer)],
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(ViewerJson.JsonMeta))]
sealed partial class ViewerJsonMetadataContext : JsonSerializerContext;
