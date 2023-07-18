using System;

namespace DHT.Server.Data;

[Flags]
public enum EmojiFlags : ushort {
	None = 0,
	Animated = 0b1
}
