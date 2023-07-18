using System.Net;
using System.Text.Json;

namespace DHT.Utils.Http;

public static class JsonExtensions {
	public static bool HasKey(this JsonElement json, string key) {
		return json.TryGetProperty(key, out _);
	}

	public static JsonElement RequireObject(this JsonElement json, string key, string? path = null) {
		if (json.TryGetProperty(key, out var result)) {
			return result;
		}
		else {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + (path == null ? key : path + '.' + key) + "' to be an object.");
		}
	}

	public static JsonElement.ArrayEnumerator RequireArray(this JsonElement json, string key, string? path = null) {
		if (json.TryGetProperty(key, out var result) && result.ValueKind == JsonValueKind.Array) {
			return result.EnumerateArray();
		}
		else {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + (path == null ? key : path + '.' + key) + "' to be an array.");
		}
	}

	public static string RequireString(this JsonElement json, string key, string path) {
		if (json.TryGetProperty(key, out var result) && result.ValueKind == JsonValueKind.String) {
			return result.ToString();
		}
		else {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be a string.");
		}
	}

	public static bool RequireBool(this JsonElement json, string key, string path) {
		if (json.TryGetProperty(key, out var result) && result.ValueKind is JsonValueKind.True or JsonValueKind.False) {
			return result.GetBoolean();
		}
		else {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be a boolean.");
		}
	}

	public static int RequireInt(this JsonElement json, string key, string path, int min = int.MinValue, int max = int.MaxValue) {
		if (json.TryGetProperty(key, out var result) && result.ValueKind == JsonValueKind.Number && result.TryGetInt32(out var i) && i >= min && i <= max) {
			return i;
		}
		else if (min == int.MinValue && max == int.MaxValue) {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be a 32-bit integer.");
		}
		else if (max == int.MaxValue) {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be a 32-bit integer (> " + min + ").");
		}
		else if (min == int.MinValue) {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be a 32-bit integer (< " + max + ").");
		}
		else {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be an integer (between " + min + " and " + max + ").");
		}
	}

	public static long RequireLong(this JsonElement json, string key, string path, long min = long.MinValue, long max = long.MaxValue) {
		if (json.TryGetProperty(key, out var result) && result.ValueKind == JsonValueKind.Number && result.TryGetInt64(out var l) && l >= min && l <= max) {
			return l;
		}
		else if (min == long.MinValue && max == long.MaxValue) {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be a 64-bit integer.");
		}
		else if (max == long.MaxValue) {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be a 64-bit integer (> " + min + ").");
		}
		else if (min == long.MinValue) {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be a 64-bit integer (< " + max + ").");
		}
		else {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be an integer (between " + min + " and " + max + ").");
		}
	}

	public static ulong RequireSnowflake(this JsonElement json, string key, string path) {
		if (ulong.TryParse(json.RequireString(key, path), out var snowflake)) {
			return snowflake;
		}
		else {
			throw new HttpException(HttpStatusCode.BadRequest, "Expected key '" + path + '.' + key + "' to be a Snowflake ID (64-bit unsigned integer in a string).");
		}
	}
}
