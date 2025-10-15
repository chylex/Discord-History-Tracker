using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Utils.Observables;

namespace DHT.Server.Database.Repositories;

public interface IChannelRepository {
	ObservableValue<long> TotalCount { get; }
	
	Task Add(IReadOnlyList<Channel> channels);
	
	Task<long> Count(CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<Channel> Get(CancellationToken cancellationToken = default);
	
	Task<int> RemoveUnreachable();
	
	internal sealed class Dummy : IChannelRepository {
		public ObservableValue<long> TotalCount { get; } = new (0L);
		
		public Task Add(IReadOnlyList<Channel> channels) {
			return Task.CompletedTask;
		}
		
		public Task<long> Count(CancellationToken cancellationToken) {
			return Task.FromResult(0L);
		}
		
		public IAsyncEnumerable<Channel> Get(CancellationToken cancellationToken) {
			return AsyncEnumerable.Empty<Channel>();
		}
		
		public Task<int> RemoveUnreachable() {
			return Task.FromResult(0);
		}
	}
}
