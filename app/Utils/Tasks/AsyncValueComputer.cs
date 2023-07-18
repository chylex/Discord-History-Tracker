using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace DHT.Utils.Tasks;

public sealed class AsyncValueComputer<TValue> {
	private readonly Action<TValue> resultProcessor;
	private readonly TaskScheduler resultTaskScheduler;
	private readonly bool processOutdatedResults;

	private readonly object stateLock = new ();

	private SoftHardCancellationToken? currentCancellationTokenSource;
	private bool wasHardCancelled = false;

	private Func<TValue>? currentComputeFunction;
	private bool hasComputeFunctionChanged = false;

	private AsyncValueComputer(Action<TValue> resultProcessor, TaskScheduler resultTaskScheduler, bool processOutdatedResults) {
		this.resultProcessor = resultProcessor;
		this.resultTaskScheduler = resultTaskScheduler;
		this.processOutdatedResults = processOutdatedResults;
	}

	public void Cancel() {
		lock (stateLock) {
			wasHardCancelled = true;
			currentCancellationTokenSource?.RequestHardCancellation();
		}
	}

	public void Compute(Func<TValue> func) {
		lock (stateLock) {
			wasHardCancelled = false;

			if (currentComputeFunction != null) {
				currentComputeFunction = func;
				hasComputeFunctionChanged = true;
				currentCancellationTokenSource?.RequestSoftCancellation();
			}
			else {
				EnqueueComputation(func);
			}
		}
	}

	[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
	private void EnqueueComputation(Func<TValue> func) {
		var cancellationTokenSource = new SoftHardCancellationToken();

		currentCancellationTokenSource?.RequestSoftCancellation();
		currentCancellationTokenSource = cancellationTokenSource;
		currentComputeFunction = func;
		hasComputeFunctionChanged = false;

		var task = Task.Run(func);

		task.ContinueWith(t => {
			if (!cancellationTokenSource.IsCancelled(processOutdatedResults)) {
				resultProcessor(t.Result);
			}
		}, CancellationToken.None, TaskContinuationOptions.NotOnFaulted, resultTaskScheduler);

		task.ContinueWith(_ => {
			lock (stateLock) {
				cancellationTokenSource.Dispose();

				if (currentCancellationTokenSource == cancellationTokenSource) {
					currentCancellationTokenSource = null;
				}

				if (hasComputeFunctionChanged && !wasHardCancelled) {
					EnqueueComputation(currentComputeFunction);
				}
				else {
					currentComputeFunction = null;
					hasComputeFunctionChanged = false;
				}
			}
		});
	}

	public sealed class Single {
		private readonly AsyncValueComputer<TValue> baseComputer;
		private readonly Func<TValue> resultComputer;

		internal Single(AsyncValueComputer<TValue> baseComputer, Func<TValue> resultComputer) {
			this.baseComputer = baseComputer;
			this.resultComputer = resultComputer;
		}

		public void Recompute() {
			baseComputer.Compute(resultComputer);
		}
	}

	public static Builder WithResultProcessor(Action<TValue> resultProcessor, TaskScheduler? scheduler = null) {
		return new Builder(resultProcessor, scheduler ?? TaskScheduler.FromCurrentSynchronizationContext());
	}

	public sealed class Builder {
		private readonly Action<TValue> resultProcessor;
		private readonly TaskScheduler resultTaskScheduler;
		private bool processOutdatedResults;

		internal Builder(Action<TValue> resultProcessor, TaskScheduler resultTaskScheduler) {
			this.resultProcessor = resultProcessor;
			this.resultTaskScheduler = resultTaskScheduler;
		}

		public Builder WithOutdatedResults() {
			this.processOutdatedResults = true;
			return this;
		}

		public AsyncValueComputer<TValue> Build() {
			return new AsyncValueComputer<TValue>(resultProcessor, resultTaskScheduler, processOutdatedResults);
		}

		public Single BuildWithComputer(Func<TValue> resultComputer) {
			return new Single(Build(), resultComputer);
		}
	}
}
