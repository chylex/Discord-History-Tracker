using System;
using System.Threading;
using System.Threading.Tasks;

namespace DHT.Utils.Tasks;

public sealed class RestartableTask<T>(Action<T> resultProcessor, TaskScheduler resultScheduler) {
	private readonly object monitor = new ();
	
	private CancellationTokenSource? cancellationTokenSource;
	
	public void Restart(Func<CancellationToken, Task<T>> resultComputer) {
		lock (monitor) {
			Cancel();
			
			cancellationTokenSource = new CancellationTokenSource();
			
			CancellationTokenSource? taskCancellationTokenSource = cancellationTokenSource;
			CancellationToken taskCancellationToken = taskCancellationTokenSource.Token;
			
			Task.Run(() => resultComputer(taskCancellationToken), taskCancellationToken)
			    .ContinueWith(task => resultProcessor(task.Result), taskCancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, resultScheduler)
			    .ContinueWith(_ => OnTaskFinished(taskCancellationTokenSource), CancellationToken.None);
		}
	}
	
	public void Cancel() {
		lock (monitor) {
			if (cancellationTokenSource != null) {
				cancellationTokenSource.Cancel();
				cancellationTokenSource = null;
			}
		}
	}
	
	private void OnTaskFinished(CancellationTokenSource taskCancellationTokenSource) {
		lock (monitor) {
			taskCancellationTokenSource.Dispose();
			
			if (cancellationTokenSource == taskCancellationTokenSource) {
				cancellationTokenSource = null;
			}
		}
	}
}
