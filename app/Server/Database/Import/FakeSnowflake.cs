using System;

namespace DHT.Server.Database.Import;

/// <summary>
/// https://discord.com/developers/docs/reference#snowflakes
/// </summary>
public sealed class FakeSnowflake {
	private const ulong DiscordEpoch = 1420070400000UL;

	private ulong id;

	public FakeSnowflake() {
		var unixMillis = (ulong) (DateTime.UtcNow.Subtract(DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerMillisecond);
		this.id = (unixMillis - DiscordEpoch) << 22;
	}

	internal ulong Next() {
		return id++;
	}
}
