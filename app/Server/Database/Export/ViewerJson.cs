using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DHT.Server.Database.Export;

static class ViewerJson {
	public sealed class JsonMeta {
		public required Dictionary<Snowflake, JsonUser> Users { get; init; }
		public required Dictionary<Snowflake, JsonServer> Servers { get; init; }
		public required Dictionary<Snowflake, JsonChannel> Channels { get; init; }
	}
	
	public sealed class JsonUser {
		public required string Name { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? DisplayName { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Avatar { get; init; }
	}
	
	public sealed class JsonServer {
		public required string Name { get; init; }
		public required string Type { get; init; }
	}
	
	public sealed class JsonChannel {
		public required Snowflake Server { get; init; }
		public required string Name { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Parent { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? Position { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Topic { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public bool? Nsfw { get; init; }
	}
	
	public sealed class JsonMessage {
		public required Snowflake Id { get; init; }
		public required Snowflake C { get; init; }
		public required Snowflake U { get; init; }
		public required long T { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? M { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public long? Te { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? R { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public JsonMessageAttachment[]? A { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string[]? E { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public JsonMessageReaction[]? Re { get; init; }
	}
	
	public sealed class JsonMessageAttachment {
		public required string Url { get; init; }
		public required string Name { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? Width { get; set; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? Height { get; set; }
	}
	
	public sealed class JsonMessageReaction {
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Id { get; init; }
		
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? N { get; init; }
		
		public required bool A { get; init; }
		public required int C { get; init; }
	}
}
