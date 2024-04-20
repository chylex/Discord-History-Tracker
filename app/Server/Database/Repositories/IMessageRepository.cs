using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;

namespace DHT.Server.Database.Repositories;

public interface IMessageRepository {
	IObservable<long> TotalCount { get; }
	
	Task Add(IReadOnlyList<Message> messages);
	
	Task<long> Count(MessageFilter? filter = null, CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<Message> Get(MessageFilter? filter = null, CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<ulong> GetIds(MessageFilter? filter = null);
	
	Task Remove(MessageFilter filter, FilterRemovalMode mode);

	internal sealed class Dummy : IMessageRepository {
		public IObservable<long> TotalCount { get; } = Observable.Return(0L);

		public Task Add(IReadOnlyList<Message> messages) {
			return Task.CompletedTask;
		}

		public Task<long> Count(MessageFilter? filter, CancellationToken cancellationToken) {
			return Task.FromResult(0L);
		}

		public IAsyncEnumerable<Message> Get(MessageFilter? filter, CancellationToken cancellationToken) {
			return AsyncEnumerable.Empty<Message>();
		}

		public IAsyncEnumerable<ulong> GetIds(MessageFilter? filter) {
			return AsyncEnumerable.Empty<ulong>();
		}

		public Task Remove(MessageFilter filter, FilterRemovalMode mode) {
			return Task.CompletedTask;
		}
	}
}
