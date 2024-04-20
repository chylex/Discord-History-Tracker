using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;

namespace DHT.Server.Database.Repositories;

public interface IChannelRepository {
	IObservable<long> TotalCount { get; }
	
	Task Add(IReadOnlyList<Channel> channels);
	
	Task<long> Count(CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<Channel> Get(CancellationToken cancellationToken = default);

	internal sealed class Dummy : IChannelRepository {
		public IObservable<long> TotalCount { get; } = Observable.Return(0L);

		public Task Add(IReadOnlyList<Channel> channels) {
			return Task.CompletedTask;
		}

		public Task<long> Count(CancellationToken cancellationToken) {
			return Task.FromResult(0L);
		}

		public IAsyncEnumerable<Channel> Get(CancellationToken cancellationToken) {
			return AsyncEnumerable.Empty<Channel>();
		}
	}
}
