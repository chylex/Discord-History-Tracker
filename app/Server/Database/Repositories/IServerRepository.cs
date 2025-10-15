using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Utils.Observables;

namespace DHT.Server.Database.Repositories;

public interface IServerRepository {
	ObservableValue<long> TotalCount { get; }
	
	Task Add(IReadOnlyList<Data.Server> servers);
	
	Task<long> Count(CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<Data.Server> Get(CancellationToken cancellationToken = default);
	
	Task<int> RemoveUnreachable();
	
	internal sealed class Dummy : IServerRepository {
		public ObservableValue<long> TotalCount { get; } = new (0L);
		
		public Task Add(IReadOnlyList<Data.Server> servers) {
			return Task.CompletedTask;
		}
		
		public Task<long> Count(CancellationToken cancellationToken) {
			return Task.FromResult(0L);
		}
		
		public IAsyncEnumerable<Data.Server> Get(CancellationToken cancellationToken) {
			return AsyncEnumerable.Empty<Data.Server>();
		}
		
		public Task<int> RemoveUnreachable() {
			return Task.FromResult(0);
		}
	}
}
