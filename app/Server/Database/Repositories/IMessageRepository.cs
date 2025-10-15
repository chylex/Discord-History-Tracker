using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data;
using DHT.Server.Data.Filters;
using DHT.Utils.Observables;

namespace DHT.Server.Database.Repositories;

public interface IMessageRepository {
	ObservableValue<long> TotalCount { get; }
	
	Task Add(IReadOnlyList<Message> messages);
	
	Task<long> Count(MessageFilter? filter = null, CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<Message> Get(MessageFilter? filter = null, CancellationToken cancellationToken = default);
	
	IAsyncEnumerable<ulong> GetIds(MessageFilter? filter = null);
	
	Task<int> Remove(MessageFilter filter, FilterRemovalMode mode);
	
	Task<int> RemoveUnreachableAttachments();
	
	internal sealed class Dummy : IMessageRepository {
		public ObservableValue<long> TotalCount { get; } = new (0L);
		
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
		
		public Task<int> Remove(MessageFilter filter, FilterRemovalMode mode) {
			return Task.FromResult(0);
		}
		
		public Task<int> RemoveUnreachableAttachments() {
			return Task.FromResult(0);
		}
	}
}
