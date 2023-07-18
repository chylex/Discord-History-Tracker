using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace DHT.Utils.Logging;

public sealed class Log {
	public static bool IsDebugEnabled { get; set; }

	static Log() {
		#if DEBUG
		IsDebugEnabled = true;
		#endif
	}

	public static Log ForType<T>() {
		return ForType(typeof(T));
	}

	public static Log ForType(Type type) {
		return new Log(type.Name);
	}

	public static Log ForType<T>(string context) {
		return ForType(typeof(T), context);
	}

	public static Log ForType(Type type, string context) {
		return new Log(type.Name, context);
	}

	private readonly string tag;
	private readonly string? context;

	private Log(string tag, string? context = null) {
		this.tag = tag;
		this.context = context;
	}

	private void FormatTags(StringBuilder builder) {
		builder.Append('[').Append(tag).Append("] ");

		if (context != null) {
			builder.Append('[').Append(context).Append("] ");
		}
	}

	private void LogLevel(ConsoleColor color, string level, string text) {
		ConsoleColor prevColor = Console.ForegroundColor;
		Console.ForegroundColor = color;

		StringBuilder builder = new StringBuilder();

		foreach (string line in text.Replace("\r", "").Split('\n')) {
			builder.Clear();
			builder.Append('[').Append(level).Append("] ");
			FormatTags(builder);
			builder.Append(line);

			string formatted = builder.ToString();
			Console.WriteLine(formatted);
			Trace.WriteLine(formatted);
		}

		Console.ForegroundColor = prevColor;
	}

	public void Debug(string message) {
		if (IsDebugEnabled) {
			LogLevel(ConsoleColor.Gray, "DEBUG", message);
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

	public Perf Start(string? context = null, [CallerMemberName] string callerMemberName = "") {
		return Perf.Start(this, context, callerMemberName);
	}
}
