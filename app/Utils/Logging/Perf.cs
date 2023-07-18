using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DHT.Utils.Logging;

public sealed class Perf {
	internal static Perf Start(Log log, string? context = null, [CallerMemberName] string callerMemberName = "") {
		return new Perf(log, callerMemberName, context);
	}

	private readonly Log log;
	private readonly string method;
	private readonly string? context;
	private readonly Stopwatch totalStopwatch;
	private readonly Stopwatch stepStopwatch;

	private Perf(Log log, string method, string? context) {
		this.log = log;
		this.method = method;
		this.context = context;
		this.totalStopwatch = new Stopwatch();
		this.totalStopwatch.Start();
		this.stepStopwatch = new Stopwatch();
		this.stepStopwatch.Start();
	}

	public void Step(string name) {
		stepStopwatch.Stop();

		if (Log.IsDebugEnabled) {
			string ctx = context == null ? string.Empty : " " + context;
			log.Debug($"Finished step '{name}' of '{method}'{ctx} in {stepStopwatch.ElapsedMilliseconds} ms.");
		}

		stepStopwatch.Restart();
	}

	public void End() {
		totalStopwatch.Stop();
		stepStopwatch.Stop();

		if (Log.IsDebugEnabled) {
			string ctx = context == null ? string.Empty : " " + context;
			log.Debug($"Finished '{method}'{ctx} in {totalStopwatch.ElapsedMilliseconds} ms.");
		}
	}
}
