using System;
using System.Diagnostics;

namespace DHT.Server.Logging {
	public static class Log {
		private static void LogLevel(ConsoleColor color, string level, string text) {
			Console.ForegroundColor = color;
			foreach (string line in text.Replace("\r", "").Split('\n')) {
				string formatted = $"[{level}] {line}";
				Console.WriteLine(formatted);
				Trace.WriteLine(formatted);
			}
		}

		public static void Info(string message) {
			LogLevel(ConsoleColor.Blue, "INFO", message);
		}

		public static void Warn(string message) {
			LogLevel(ConsoleColor.Yellow, "WARN", message);
		}

		public static void Error(string message) {
			LogLevel(ConsoleColor.Red, "ERROR", message);
		}

		public static void Error(Exception e) {
			LogLevel(ConsoleColor.Red, "ERROR", e.ToString());
		}
	}
}
