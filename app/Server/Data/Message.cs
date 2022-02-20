using System.Collections.Immutable;

namespace DHT.Server.Data {
	public readonly struct Message {
		public ulong Id { get; internal init; }
		public ulong Sender { get; internal init; }
		public ulong Channel { get; internal init; }
		public string Text { get; internal init; }
		public long Timestamp { get; internal init; }
		public long? EditTimestamp { get; internal init; }
		public ulong? RepliedToId { get; internal init; }
		public ImmutableArray<Attachment> Attachments { get; internal init; }
		public ImmutableArray<Embed> Embeds { get; internal init; }
		public ImmutableArray<Reaction> Reactions { get; internal init; }
	}
}
