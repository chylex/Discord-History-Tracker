using System;
using System.Threading;
using System.Threading.Tasks;
using DHT.Utils.Observables;

namespace DHT.Desktop.Common;

static class AvaloniaObsevableValueExtensions {
	public static IDisposable SubscribeLastOnUI<T>(this ObservableValue<T> observable, Action<T> action, TimeSpan delayBetweenRuns) {
		Task Action(T value, CancellationToken cancellationToken) {
			action(value);
			return Task.Delay(delayBetweenRuns, cancellationToken);
		}
		
		return observable.SubscribeLast(Action, TaskScheduler.FromCurrentSynchronizationContext());
	}
}
