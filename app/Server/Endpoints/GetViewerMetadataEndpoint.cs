using System;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Database.Export;
using DHT.Server.Service.Viewer;
using Sisk.Core.Http;
using Sisk.Core.Http.Streams;

namespace DHT.Server.Endpoints;

sealed class GetViewerMetadataEndpoint(IDatabaseFile db, ViewerSessions viewerSessions) : BaseEndpoint {
	protected override async Task<HttpResponse> Respond(HttpRequest request) {
		Guid sessionId = GetSessionId(request);
		ViewerSession session = viewerSessions.Get(sessionId);
		
		HttpResponseStreamManager response = request.GetResponseStream();
		response.SendChunked = true;
		response.SetStatus(HttpStatusCode.OK);
		response.SetHeader(HttpKnownHeaderNames.ContentType, MediaTypeNames.Application.Json);
		
		await ViewerJsonExport.GetMetadata(response.ResponseStream, db, session.MessageFilter, CancellationToken.None);
		
		return response.Close();
	}
}
