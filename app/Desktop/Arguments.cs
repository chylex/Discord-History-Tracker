using System;
using DHT.Server.Logging;

namespace DHT.Desktop {
	public class Arguments {
		public static Arguments Empty => new(Array.Empty<string>());

		public string? DatabaseFile { get; }
		public ushort? ServerPort { get; }
		public string? ServerToken { get; }

		public Arguments(string[] args) {
			for (int i = 0; i < args.Length; i++) {
				string key = args[i];

				if (i >= args.Length - 1) {
					Log.Warn("Missing value for command line argument: " + key);
					continue;
				}

				string value = args[++i];

				switch (key) {
					case "-db":
						DatabaseFile = value;
						continue;

					case "-port": {
						if (ushort.TryParse(value, out var port)) {
							ServerPort = port;
						}
						else {
							Log.Warn("Invalid port number: " + value);
						}

						continue;
					}

					case "-token":
						ServerToken = value;
						continue;

					default:
						Log.Warn("Unknown command line argument: " + key);
						break;
				}
			}
		}
	}
}
