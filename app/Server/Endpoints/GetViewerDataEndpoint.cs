using System;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Database.Export;
using DHT.Server.Service.Viewer;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints; 

sealed class GetViewerDataEndpoint(IDatabaseFile db, ViewerSessions viewerSessions) : BaseEndpoint(db) {
	protected override async Task Respond(HttpRequest request, HttpResponse response) {
		if (!request.Query.TryGetValue("session", out var sessionIdValue) || sessionIdValue.Count != 1 || !Guid.TryParse(sessionIdValue[0], out Guid sessionId)) {
			throw new HttpException(HttpStatusCode.BadRequest, "Invalid session ID.");
		}
		
		response.ContentType = MediaTypeNames.Application.Json;
		
		var session = viewerSessions.Get(sessionId);
		await ViewerJsonExport.Generate(response.Body, Db, session.MessageFilter);
	}
}
