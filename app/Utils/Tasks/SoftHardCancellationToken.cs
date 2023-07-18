using System;
using System.Threading;

namespace DHT.Utils.Tasks;

/// <summary>
/// Manages a pair of cancellation tokens that follow these rules:
/// <list type="number">
/// <item><description>If the soft token is cancelled, the hard token remains uncancelled.</description></item>
/// <item><description>If the hard token is cancelled, the soft token is also cancelled.</description></item>
/// </list>
/// </summary>
sealed class SoftHardCancellationToken : IDisposable {
	private readonly CancellationTokenSource soft;
	private readonly CancellationTokenSource hard;

	public SoftHardCancellationToken() {
		this.soft = new CancellationTokenSource();
		this.hard = new CancellationTokenSource();
	}

	public bool IsCancelled(bool onlyHardCancellation) {
		return (onlyHardCancellation ? hard : soft).IsCancellationRequested;
	}

	public void RequestSoftCancellation() {
		soft.Cancel();
	}

	public void RequestHardCancellation() {
		soft.Cancel();
		hard.Cancel();
	}

	public void Dispose() {
		soft.Dispose();
		hard.Dispose();
	}
}
