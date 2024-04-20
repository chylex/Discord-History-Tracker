using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Database.Export;
using DHT.Server.Service.Viewer;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints; 

sealed class GetViewerMessagesEndpoint(IDatabaseFile db, ViewerSessions viewerSessions) : BaseEndpoint(db) {
	protected override Task Respond(HttpRequest request, HttpResponse response, CancellationToken cancellationToken) {
		var sessionId = GetSessionId(request);
		var session = viewerSessions.Get(sessionId);
		
		response.ContentType = "application/x-ndjson";
		return ViewerJsonExport.GetMessages(response.Body, Db, session.MessageFilter, cancellationToken);
	}
}
