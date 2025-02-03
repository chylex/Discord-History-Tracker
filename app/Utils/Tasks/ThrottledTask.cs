using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DHT.Utils.Logging;

namespace DHT.Utils.Tasks;

public abstract class ThrottledTaskBase<T> : IDisposable {
	private readonly Channel<Func<CancellationToken, T>> taskChannel = Channel.CreateBounded<Func<CancellationToken, T>>(new BoundedChannelOptions(capacity: 1) {
		SingleReader = true,
		SingleWriter = false,
		AllowSynchronousContinuations = false,
		FullMode = BoundedChannelFullMode.DropOldest,
	});
	
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private readonly Log log;
	
	internal ThrottledTaskBase(Log log) {
		this.log = log;
	}
	
	protected async Task ReaderTask() {
		CancellationToken cancellationToken = cancellationTokenSource.Token;
		
		try {
			await foreach (Func<CancellationToken, T> item in taskChannel.Reader.ReadAllAsync(cancellationToken)) {
				try {
					await Run(item, cancellationToken);
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
	
	protected abstract Task Run(Func<CancellationToken, T> func, CancellationToken cancellationToken);
	
	public void Post(Func<CancellationToken, T> resultComputer) {
		taskChannel.Writer.TryWrite(resultComputer);
	}
	
	public void Dispose() {
		taskChannel.Writer.Complete();
		cancellationTokenSource.Cancel();
	}
}

public sealed class ThrottledTask : ThrottledTaskBase<Task> {
	private readonly Action resultProcessor;
	private readonly TaskScheduler resultScheduler;
	
	public ThrottledTask(Log log, Action resultProcessor, TaskScheduler resultScheduler) : base(log) {
		this.resultProcessor = resultProcessor;
		this.resultScheduler = resultScheduler;
		
		Task.Run(ReaderTask);
	}
	
	protected override async Task Run(Func<CancellationToken, Task> func, CancellationToken cancellationToken) {
		await func(cancellationToken);
		await Task.Factory.StartNew(resultProcessor, cancellationToken, TaskCreationOptions.None, resultScheduler);
	}
}

public sealed class ThrottledTask<T> : ThrottledTaskBase<Task<T>> {
	private readonly Action<T> resultProcessor;
	private readonly TaskScheduler resultScheduler;
	
	public ThrottledTask(Log log, Action<T> resultProcessor, TaskScheduler resultScheduler) : base(log) {
		this.resultProcessor = resultProcessor;
		this.resultScheduler = resultScheduler;
		
		Task.Run(ReaderTask);
	}
	
	protected override async Task Run(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken) {
		T result = await func(cancellationToken);
		await Task.Factory.StartNew(() => resultProcessor(result), cancellationToken, TaskCreationOptions.None, resultScheduler);
	}
}
