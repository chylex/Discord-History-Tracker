using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DHT.Server.Database.Repositories;

public interface IServerRepository {
	Task Add(IReadOnlyList<Data.Server> servers);
	
	IAsyncEnumerable<Data.Server> Get();

	internal sealed class Dummy : IServerRepository {
		public Task Add(IReadOnlyList<Data.Server> servers) {
			return Task.CompletedTask;
		}

		public IAsyncEnumerable<Data.Server> Get() {
			return AsyncEnumerable.Empty<Data.Server>();
		}
	}
}
