using System.Collections.Generic;
using DHT.Utils.Logging;

namespace DHT.Desktop;

sealed class Arguments {
	private static readonly Log Log = Log.ForType<Arguments>();
	
	private const int FirstArgument = 1;
	
	public static Arguments Empty => new ([]);
	
	public bool Console { get; }
	public string? DatabaseFile { get; }
	public ushort? ServerPort { get; }
	public string? ServerToken { get; }
	public byte? ConcurrentDownloads { get; }
	
	public Arguments(IReadOnlyList<string> args) {
		for (int i = FirstArgument; i < args.Count; i++) {
			string key = args[i];
			
			switch (key) {
				case "-debug":
					Log.IsDebugEnabled = true;
					continue;
				
				case "-console":
					Console = true;
					continue;
			}
			
			string value;
			
			if (i == FirstArgument && !key.StartsWith('-')) {
				value = key;
				key = "-db";
			}
			else if (i >= args.Count - 1) {
				Log.Warn("Missing value for command line argument: " + key);
				continue;
			}
			else {
				value = args[++i];
			}
			
			switch (key) {
				case "-db":
					DatabaseFile = value;
					continue;
				
				case "-port": {
					if (!ushort.TryParse(value, out ushort port)) {
						Log.Warn("Invalid port number: " + value);
					}
					else {
						ServerPort = port;
					}
					
					continue;
				}
				
				case "-token":
					ServerToken = value;
					continue;
				
				case "-concurrentdownloads":
					if (!ulong.TryParse(value, out ulong concurrentDownloads) || concurrentDownloads == 0) {
						Log.Warn("Invalid concurrent downloads count: " + value);
					}
					else if (concurrentDownloads > 10) {
						Log.Warn("Limiting concurrent downloads to 10.");
						ConcurrentDownloads = 10;
					}
					else {
						ConcurrentDownloads = (byte) concurrentDownloads;
					}
					
					continue;
				
				default:
					Log.Warn("Unknown command line argument: " + key);
					break;
			}
		}
	}
}
