using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DHT.Utils.Observables;

public sealed class ObservableValue<T>(T? value) {
	private readonly List<LastValueObserver<T>> observers = [];
	private T? value = value;
	
	public void Set(T value) {
		lock (this) {
			if (EqualityComparer<T>.Default.Equals(value, this.value)) {
				return;
			}
			
			this.value = value;
			
			foreach (var observer in observers) {
				observer.Notify(value);
			}
		}
	}
	
	public IDisposable SubscribeLast(Func<T, CancellationToken, Task> action, TaskScheduler scheduler) {
		var observer = new LastValueObserver<T>(this, action, scheduler);
		
		lock (this) {
			observers.Add(observer);
			
			if (value is not null) {
				observer.Notify(value);
			}
		}
		
		return observer;
	}
	
	internal void Unsubscribe(LastValueObserver<T> observer) {
		lock (this) {
			observers.Remove(observer);
		}
	}
}
