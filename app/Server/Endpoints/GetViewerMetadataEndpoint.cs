using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Database.Export;
using DHT.Server.Service.Viewer;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class GetViewerMetadataEndpoint(IDatabaseFile db, ViewerSessions viewerSessions) : BaseEndpoint {
	protected override Task Respond(HttpRequest request, HttpResponse response, CancellationToken cancellationToken) {
		Guid sessionId = GetSessionId(request);
		ViewerSession session = viewerSessions.Get(sessionId);
		
		response.ContentType = MediaTypeNames.Application.Json;
		return ViewerJsonExport.GetMetadata(response.Body, db, session.MessageFilter, cancellationToken);
	}
}
