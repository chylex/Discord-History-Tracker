using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Data.Filters;

namespace DHT.Server.Database.Repositories;

public interface IAttachmentRepository {
	IObservable<long> TotalCount { get; }

	Task<long> Count(AttachmentFilter? filter = null, CancellationToken cancellationToken = default);

	internal sealed class Dummy : IAttachmentRepository {
		public IObservable<long> TotalCount { get; } = Observable.Return(0L);
		
		public Task<long> Count(AttachmentFilter? filter = null, CancellationToken cancellationToken = default) {
			return Task.FromResult(0L);
		}
	}
}
