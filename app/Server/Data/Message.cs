using System.Collections.Immutable;

namespace DHT.Server.Data;

public readonly struct Message {
	public ulong Id { get; init; }
	public ulong Sender { get; init; }
	public ulong Channel { get; init; }
	public string Text { get; init; }
	public long Timestamp { get; init; }
	public long? EditTimestamp { get; init; }
	public ulong? RepliedToId { get; init; }
	public ImmutableArray<Attachment> Attachments { get; init; }
	public ImmutableArray<Embed> Embeds { get; init; }
	public ImmutableArray<Reaction> Reactions { get; init; }
}
