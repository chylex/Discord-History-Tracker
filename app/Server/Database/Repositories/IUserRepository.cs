using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Utils.Observables;

namespace DHT.Server.Database.Repositories;

public interface IUserRepository {
	ObservableValue<long> TotalCount { get; }
	
	Task Add(IReadOnlyList<User> users);
	
	Task<long> Count(CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<User> Get(CancellationToken cancellationToken = default);
	
	Task<int> RemoveUnreachable();
	
	internal sealed class Dummy : IUserRepository {
		public ObservableValue<long> TotalCount { get; } = new (0L);
		
		public Task Add(IReadOnlyList<User> users) {
			return Task.CompletedTask;
		}
		
		public Task<long> Count(CancellationToken cancellationToken) {
			return Task.FromResult(0L);
		}
		
		public IAsyncEnumerable<User> Get(CancellationToken cancellationToken) {
			return AsyncEnumerable.Empty<User>();
		}
		
		public Task<int> RemoveUnreachable() {
			return Task.FromResult(0);
		}
	}
}
