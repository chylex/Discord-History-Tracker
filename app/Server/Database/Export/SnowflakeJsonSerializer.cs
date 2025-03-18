using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DHT.Server.Database.Export;

sealed class SnowflakeJsonSerializer : JsonConverter<Snowflake> {
	private const int MaxUlongStringLength = 20;
	
	public override Snowflake Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		return new Snowflake(ulong.Parse(reader.GetString()!));
	}
	
	public override void Write(Utf8JsonWriter writer, Snowflake value, JsonSerializerOptions options) {
		writer.WriteStringValue(Format(value, stackalloc byte[MaxUlongStringLength]));
	}
	
	public override Snowflake ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		return new Snowflake(ulong.Parse(reader.GetString()!));
	}
	
	public override void WriteAsPropertyName(Utf8JsonWriter writer, Snowflake value, JsonSerializerOptions options) {
		writer.WritePropertyName(Format(value, stackalloc byte[MaxUlongStringLength]));
	}
	
	private static ReadOnlySpan<byte> Format(Snowflake value, Span<byte> destination) {
		if (!Utf8Formatter.TryFormat(value.Id, destination, out int bytesWritten)) {
			Debug.Fail("Failed to format Snowflake value.");
		}
		
		return destination[..bytesWritten];
	}
}
