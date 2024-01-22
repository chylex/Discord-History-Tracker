using System;
using System.Collections.Generic;

namespace DHT.Server.Service.Viewer;

public sealed class ViewerSessions : IDisposable {
	private readonly Dictionary<Guid, ViewerSession> sessions = new ();
	private bool isDisposed = false;

	public Guid Register(ViewerSession session) {
		Guid guid = Guid.NewGuid();
		
		lock (this) {
			ObjectDisposedException.ThrowIf(isDisposed, this);
			sessions[guid] = session;
		}
		
		return guid;
	}

	internal ViewerSession Get(Guid guid) {
		lock (this) {
			return sessions.GetValueOrDefault(guid);
		}
	}

	public void Dispose() {
		lock (this) {
			if (!isDisposed) {
				isDisposed = true;
				sessions.Clear();
			}
		}
	}
}
