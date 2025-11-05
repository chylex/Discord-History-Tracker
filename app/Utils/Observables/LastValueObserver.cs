using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DHT.Utils.Observables;

sealed class LastValueObserver<T> : IDisposable {
	private readonly ObservableValue<T> observable;
	private readonly Func<T, CancellationToken, Task> action;
	private readonly TaskScheduler scheduler;
	
	private readonly Channel<T> channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity: 1) {
		AllowSynchronousContinuations = false,
		FullMode = BoundedChannelFullMode.DropOldest,
		SingleReader = true,
		SingleWriter = false,
	});
	
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	
	public LastValueObserver(ObservableValue<T> observable, Func<T, CancellationToken, Task> action, TaskScheduler scheduler) {
		this.observable = observable;
		this.action = action;
		this.scheduler = scheduler;
		
		_ = ReadNextValue();
	}
	
	private async Task ReadNextValue() {
		CancellationToken cancellationToken = cancellationTokenSource.Token;
		
		try {
			await foreach (T value in channel.Reader.ReadAllAsync(cancellationToken)) {
				try {
					await Task.Factory.StartNew(UseValue, value, CancellationToken.None, TaskCreationOptions.None, scheduler).WaitAsync(cancellationToken);
				} catch (Exception) {
					// Ignore.
				}
			}
		} finally {
			cancellationTokenSource.Dispose();
		}
	}
	
	private Task UseValue(object? value) {
		return action((T) value!, cancellationTokenSource.Token);
	}
	
	public void Notify(T value) {
		channel.Writer.TryWrite(value);
	}
	
	public void Dispose() {
		observable.Unsubscribe(this);
		
		try {
			cancellationTokenSource.Cancel();
		} catch (ObjectDisposedException) {}
		
		channel.Writer.TryComplete();
	}
}
