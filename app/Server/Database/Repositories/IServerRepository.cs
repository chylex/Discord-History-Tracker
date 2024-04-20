using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DHT.Server.Database.Repositories;

public interface IServerRepository {
	IObservable<long> TotalCount { get; }
	
	Task Add(IReadOnlyList<Data.Server> servers);
	
	Task<long> Count(CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<Data.Server> Get(CancellationToken cancellationToken = default);

	internal sealed class Dummy : IServerRepository {
		public IObservable<long> TotalCount { get; } = Observable.Return(0L);

		public Task Add(IReadOnlyList<Data.Server> servers) {
			return Task.CompletedTask;
		}

		public Task<long> Count(CancellationToken cancellationToken) {
			return Task.FromResult(0L);
		}

		public IAsyncEnumerable<Data.Server> Get(CancellationToken cancellationToken) {
			return AsyncEnumerable.Empty<Data.Server>();
		}
	}
}
