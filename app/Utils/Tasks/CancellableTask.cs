using System;
using System.Threading;
using System.Threading.Tasks;

namespace DHT.Utils.Tasks;

public sealed class CancellableTask : IDisposable {
	public static CancellableTask Run(Func<CancellationToken, Task> action) {
		return new CancellableTask(action);
	}
	
	public Task Task { get; }
	
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	
	private CancellableTask(Func<CancellationToken, Task> action) {
		CancellationToken cancellationToken = cancellationTokenSource.Token;
		Task = Task.Run(() => action(cancellationToken));
	}
	
	public void Cancel() {
		cancellationTokenSource.Cancel();
	}
	
	public void Dispose() {
		cancellationTokenSource.Dispose();
	}
}
