using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DHT.Utils.Collections;

public sealed class ConcurrentPool<T> {
	private readonly SemaphoreSlim mutexSemaphore;
	private readonly SemaphoreSlim availableItemSemaphore;
	private readonly Stack<T> items;
	
	public ConcurrentPool(int size) {
		mutexSemaphore = new SemaphoreSlim(1);
		availableItemSemaphore = new SemaphoreSlim(initialCount: 0, size);
		items = new Stack<T>();
	}
	
	public async Task Push(T item, CancellationToken cancellationToken) {
		await PushItem(item, cancellationToken);
		availableItemSemaphore.Release();
	}
	
	public async Task<T> Pop(CancellationToken cancellationToken) {
		await availableItemSemaphore.WaitAsync(cancellationToken);
		return await PopItem(cancellationToken);
	}
	
	private async Task PushItem(T item, CancellationToken cancellationToken) {
		await mutexSemaphore.WaitAsync(cancellationToken);
		try {
			items.Push(item);
		} finally {
			mutexSemaphore.Release();
		}
	}
	
	private async Task<T> PopItem(CancellationToken cancellationToken) {
		await mutexSemaphore.WaitAsync(cancellationToken);
		try {
			return items.Pop();
		} finally {
			mutexSemaphore.Release();
		}
	}
}
