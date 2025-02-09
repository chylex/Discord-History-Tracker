using System.Diagnostics.CodeAnalysis;

namespace DHT.Server.Data.Settings;

public static class SettingsKey {
	public static Bool SeparateFileForDownloads { get; } = new ("separate_file_for_downloads");
	public static Bool DownloadsAutoStart { get; } = new ("downloads_auto_start");
	public static Bool DownloadsLimitSize { get; } = new ("downloads_limit_size");
	public static UnsignedLong DownloadsMaximumSize { get; } = new ("downloads_maximum_size");
	public static String DownloadsMaximumSizeUnit { get; } = new ("downloads_maximum_size_unit");
	
	public sealed class String(string key) : SettingsKey<string>(key) {
		internal override bool FromString(string value, out string result) {
			result = value;
			return true;
		}
		
		internal override string ToString(string value) {
			return value;
		}
	}
	
	public sealed class Bool(string key) : SettingsKey<bool>(key) {
		internal override bool FromString(string value, out bool result) {
			switch (value) {
				case "1":
					result = true;
					return true;
				
				case "0":
					result = false;
					return true;
				
				default:
					result = false;
					return false;
			}
		}
		
		internal override string ToString(bool value) {
			return value ? "1" : "0";
		}
	}
	
	public sealed class UnsignedLong(string key) : SettingsKey<ulong>(key) {
		internal override bool FromString(string value, out ulong result) {
			return ulong.TryParse(value, out result);
		}
		
		internal override string ToString(ulong value) {
			return value.ToString();
		}
	}
}

public abstract class SettingsKey<T>(string key) {
	internal string Key => key;
	
	internal abstract bool FromString(string value, [NotNullWhen(true)] out T result);
	internal abstract string ToString(T value);
}
