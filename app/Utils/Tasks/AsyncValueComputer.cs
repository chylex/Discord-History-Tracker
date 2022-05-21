using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace DHT.Utils.Tasks {
	public sealed class AsyncValueComputer<TValue> {
		private readonly Action<TValue> resultProcessor;
		private readonly TaskScheduler resultTaskScheduler;
		private readonly bool processOutdatedResults;

		private readonly object stateLock = new ();

		private CancellationTokenSource? currentCancellationTokenSource;
		private Func<CancellationToken, TValue>? currentComputeFunction;
		private bool hasComputeFunctionChanged = false;

		private AsyncValueComputer(Action<TValue> resultProcessor, TaskScheduler resultTaskScheduler, bool processOutdatedResults) {
			this.resultProcessor = resultProcessor;
			this.resultTaskScheduler = resultTaskScheduler;
			this.processOutdatedResults = processOutdatedResults;
		}

		public void Compute(Func<CancellationToken, TValue> func) {
			lock (stateLock) {
				if (currentComputeFunction != null) {
					currentComputeFunction = func;
					hasComputeFunctionChanged = true;
					currentCancellationTokenSource?.Cancel();
				}
				else {
					EnqueueComputation(func);
				}
			}
		}

		[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
		private void EnqueueComputation(Func<CancellationToken, TValue> func) {
			var cancellationTokenSource = new CancellationTokenSource();
			var cancellationToken = cancellationTokenSource.Token;

			currentCancellationTokenSource?.Cancel();
			currentCancellationTokenSource = cancellationTokenSource;
			currentComputeFunction = func;
			hasComputeFunctionChanged = false;

			var task = Task.Run(() => func(cancellationToken));
			
			task.ContinueWith(t => {
				if (processOutdatedResults || !cancellationToken.IsCancellationRequested) {
					resultProcessor(t.Result);
				}
			}, CancellationToken.None, TaskContinuationOptions.NotOnFaulted, resultTaskScheduler);
			
			task.ContinueWith(_ => {
				lock (stateLock) {
					cancellationTokenSource.Dispose();

					if (currentCancellationTokenSource == cancellationTokenSource) {
						currentCancellationTokenSource = null;
					}

					if (hasComputeFunctionChanged) {
						EnqueueComputation(currentComputeFunction);
					}
					else {
						currentComputeFunction = null;
					}
				}
			});
		}

		public sealed class Single {
			private readonly AsyncValueComputer<TValue> baseComputer;
			private readonly Func<CancellationToken, TValue> resultComputer;

			internal Single(AsyncValueComputer<TValue> baseComputer, Func<CancellationToken, TValue> resultComputer) {
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

			public Single BuildWithComputer(Func<CancellationToken, TValue> resultComputer) {
				return new Single(Build(), resultComputer);
			}
		}
	}
}
