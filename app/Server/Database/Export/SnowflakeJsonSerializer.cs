using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DHT.Server.Database.Export;

sealed class SnowflakeJsonSerializer : JsonConverter<Snowflake> {
	public override Snowflake Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		return new Snowflake(ulong.Parse(reader.GetString()!));
	}
	
	public override void Write(Utf8JsonWriter writer, Snowflake value, JsonSerializerOptions options) {
		writer.WriteStringValue(value.Id.ToString());
	}
	
	public override Snowflake ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		return new Snowflake(ulong.Parse(reader.GetString()!));
	}
	
	public override void WriteAsPropertyName(Utf8JsonWriter writer, Snowflake value, JsonSerializerOptions options) {
		writer.WritePropertyName(value.Id.ToString());
	}
}
