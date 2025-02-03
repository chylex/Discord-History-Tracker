using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DHT.Utils.Logging;

namespace DHT.Utils.Tasks;

public sealed class DelayedThrottledTask<T> : IDisposable {
	private readonly Channel<T> taskChannel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity: 1) {
		SingleReader = true,
		SingleWriter = false,
		AllowSynchronousContinuations = false,
		FullMode = BoundedChannelFullMode.DropOldest,
	});
	
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private readonly Log log;
	private readonly TimeSpan delay;
	private readonly Func<T, Task> inputProcessor;
	
	public DelayedThrottledTask(Log log, TimeSpan delay, Func<T, Task> inputProcessor) {
		this.log = log;
		this.delay = delay;
		this.inputProcessor = inputProcessor;
		
		Task.Run(ReaderTask);
	}
	
	private async Task ReaderTask() {
		CancellationToken cancellationToken = cancellationTokenSource.Token;
		
		try {
			while (await taskChannel.Reader.WaitToReadAsync(cancellationToken)) {
				await Task.Delay(delay, cancellationToken);
				
				T input = await taskChannel.Reader.ReadAsync(cancellationToken);
				try {
					await inputProcessor(input);
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception e) {
					log.Error("Caught exception in task: " + e);
				}
			}
		} catch (OperationCanceledException) {
			// Ignore.
		} finally {
			cancellationTokenSource.Dispose();
		}
	}
	
	public void Post(T input) {
		taskChannel.Writer.TryWrite(input);
	}
	
	public void Dispose() {
		taskChannel.Writer.Complete();
		cancellationTokenSource.Cancel();
	}
}
