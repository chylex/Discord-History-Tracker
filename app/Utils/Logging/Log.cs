using System;
using System.Diagnostics;

namespace DHT.Utils.Logging {
	public sealed class Log {
		public static Log ForType<T>() {
			return ForType(typeof(T));
		}

		public static Log ForType(Type type) {
			return new Log(type.Name);
		}

		private readonly string tag;

		private Log(string tag) {
			this.tag = tag;
		}

		private void LogLevel(ConsoleColor color, string level, string text) {
			Console.ForegroundColor = color;
			foreach (string line in text.Replace("\r", "").Split('\n')) {
				string formatted = $"[{level}] [{tag}] {line}";
				Console.WriteLine(formatted);
				Trace.WriteLine(formatted);
			}
		}

		public void Info(string message) {
			LogLevel(ConsoleColor.Blue, "INFO", message);
		}

		public void Warn(string message) {
			LogLevel(ConsoleColor.Yellow, "WARN", message);
		}

		public void Error(string message) {
			LogLevel(ConsoleColor.Red, "ERROR", message);
		}

		public void Error(Exception e) {
			LogLevel(ConsoleColor.Red, "ERROR", e.ToString());
		}
	}
}
