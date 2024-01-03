using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DHT.Utils.Logging;

namespace DHT.Utils.Tasks;

public sealed class ObservableThrottledTask<T> : IObservable<T>, IDisposable {
	private readonly ReplaySubject<T> subject;
	private readonly ThrottledTask<T> task;

	public ObservableThrottledTask(Log log, TaskScheduler resultScheduler) {
		this.subject = new ReplaySubject<T>(bufferSize: 1);
		this.task = new ThrottledTask<T>(log, subject.OnNext, resultScheduler);
	}

	public void Post(Func<CancellationToken, Task<T>> resultComputer) {
		task.Post(resultComputer);
	}
	
	public IDisposable Subscribe(IObserver<T> observer) {
		return subject.Subscribe(observer);
	}

	public void Dispose() {
		task.Dispose();
		
		subject.OnCompleted();
		subject.Dispose();
	}
}
