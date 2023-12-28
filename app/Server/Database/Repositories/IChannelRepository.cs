using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DHT.Server.Data;

namespace DHT.Server.Database.Repositories;

public interface IChannelRepository {
	Task Add(IReadOnlyList<Channel> channels);
	
	IAsyncEnumerable<Channel> Get();

	internal sealed class Dummy : IChannelRepository {
		public Task Add(IReadOnlyList<Channel> channels) {
			return Task.CompletedTask;
		}

		public IAsyncEnumerable<Channel> Get() {
			return AsyncEnumerable.Empty<Channel>();
		}
	}
}
