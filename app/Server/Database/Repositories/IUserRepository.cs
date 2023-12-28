using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DHT.Server.Data;

namespace DHT.Server.Database.Repositories;

public interface IUserRepository {
	Task Add(IReadOnlyList<User> users);
	
	IAsyncEnumerable<User> Get();

	internal sealed class Dummy : IUserRepository {
		public Task Add(IReadOnlyList<User> users) {
			return Task.CompletedTask;
		}

		public IAsyncEnumerable<User> Get() {
			return AsyncEnumerable.Empty<User>();
		}
	}
}
