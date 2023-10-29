using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using DHT.Utils.Logging;
using static System.Environment.SpecialFolder;
using static System.Environment.SpecialFolderOption;

namespace DHT.Desktop.Discord;

static class DiscordAppSettings {
	private static readonly Log Log = Log.ForType(typeof(DiscordAppSettings));

	private const string JsonKeyDevTools = "DANGEROUS_ENABLE_DEVTOOLS_ONLY_ENABLE_IF_YOU_KNOW_WHAT_YOURE_DOING";

	public static string JsonFilePath { get; }
	private static string JsonBackupFilePath { get; }

	[SuppressMessage("ReSharper", "ConvertIfStatementToConditionalTernaryExpression")]
	static DiscordAppSettings() {
		string rootFolder;

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			rootFolder = Path.Combine(Environment.GetFolderPath(ApplicationData, DoNotVerify), "Discord");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			rootFolder = Path.Combine(Environment.GetFolderPath(UserProfile, DoNotVerify), "Library", "Application Support", "Discord");
		}
		else {
			rootFolder = Path.Combine(Environment.GetFolderPath(ApplicationData, DoNotVerify), "discord");
		}

		JsonFilePath = Path.Combine(rootFolder, "settings.json");
		JsonBackupFilePath = JsonFilePath + ".bak";
	}

	public static async Task<bool?> AreDevToolsEnabled() {
		try {
			return AreDevToolsEnabled(await ReadSettingsJson());
		} catch (Exception e) {
			Log.Error("Cannot read settings file.");
			Log.Error(e);
			return null;
		}
	}

	private static bool AreDevToolsEnabled(Dictionary<string, object?> json) {
		return json.TryGetValue(JsonKeyDevTools, out var value) && value is JsonElement { ValueKind: JsonValueKind.True };
	}

	public static async Task<SettingsJsonResult> ConfigureDevTools(bool enable) {
		Dictionary<string, object?> json;

		try {
			json = await ReadSettingsJson();
		} catch (FileNotFoundException) {
			return SettingsJsonResult.FileNotFound;
		} catch (JsonException) {
			return SettingsJsonResult.InvalidJson;
		} catch (Exception e) {
			Log.Error(e);
			return SettingsJsonResult.ReadError;
		}

		if (enable == AreDevToolsEnabled(json)) {
			return SettingsJsonResult.AlreadySet;
		}

		if (enable) {
			json[JsonKeyDevTools] = true;
		}
		else {
			json.Remove(JsonKeyDevTools);
		}

		try {
			if (!File.Exists(JsonBackupFilePath)) {
				File.Copy(JsonFilePath, JsonBackupFilePath);
			}

			await WriteSettingsJson(json);
		} catch (Exception e) {
			Log.Error("An error occurred when writing settings file.");
			Log.Error(e);

			if (File.Exists(JsonBackupFilePath)) {
				try {
					File.Move(JsonBackupFilePath, JsonFilePath, true);
					Log.Info("Restored settings file from backup.");
				} catch (Exception e2) {
					Log.Error("Cannot restore settings file from backup.");
					Log.Error(e2);
				}
			}

			return SettingsJsonResult.WriteError;
		}

		try {
			File.Delete(JsonBackupFilePath);
		} catch (Exception e) {
			Log.Error("Cannot delete backup file.");
			Log.Error(e);
		}

		return SettingsJsonResult.Success;
	}

	private static async Task<Dictionary<string, object?>> ReadSettingsJson() {
		await using var stream = new FileStream(JsonFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		return await JsonSerializer.DeserializeAsync<Dictionary<string, object?>?>(stream) ?? throw new JsonException();
	}

	private static async Task WriteSettingsJson(Dictionary<string, object?> json) {
		await using var stream = new FileStream(JsonFilePath, FileMode.Truncate, FileAccess.Write, FileShare.None);
		await JsonSerializer.SerializeAsync(stream, json, new JsonSerializerOptions { WriteIndented = true });
	}
}
