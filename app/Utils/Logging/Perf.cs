using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DHT.Utils.Logging {
	public sealed class Perf {
		internal static Perf Start(Log log, [CallerMemberName] string callerMemberName = "") {
			return new Perf(log, callerMemberName);
		}

		private readonly Log log;
		private readonly string method;
		private readonly Stopwatch stopwatch;

		private Perf(Log log, string method) {
			this.log = log;
			this.method = method;
			this.stopwatch = new Stopwatch();
			this.stopwatch.Start();
		}

		public void End() {
			stopwatch.Stop();

			if (Log.IsDebugEnabled) {
				log.Debug($"Finished '{method}' in {stopwatch.ElapsedMilliseconds} ms.");
			}
		}
	}
}
