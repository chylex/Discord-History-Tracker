using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Http;
using DHT.Utils.Resources;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class GetTrackingScriptEndpoint(IDatabaseFile db, ServerParameters parameters, ResourceLoader resources) : BaseEndpoint(db) {
	protected override async Task Respond(HttpRequest request, HttpResponse response, CancellationToken cancellationToken) {
		string bootstrap = await resources.ReadTextAsync("Tracker/bootstrap.js");
		string script = bootstrap.Replace("= 0; /*[PORT]*/", "= " + parameters.Port + ";")
		                         .Replace("/*[TOKEN]*/", HttpUtility.JavaScriptStringEncode(parameters.Token))
		                         .Replace("/*[IMPORTS]*/", await resources.ReadJoinedAsync("Tracker/scripts/", '\n', [ "/webpack.js" ]))
		                         .Replace("/*[CSS-CONTROLLER]*/", await resources.ReadTextAsync("Tracker/styles/controller.css"))
		                         .Replace("/*[CSS-SETTINGS]*/", await resources.ReadTextAsync("Tracker/styles/settings.css"))
		                         .Replace("/*[DEBUGGER]*/", request.Query.ContainsKey("debug") ? "debugger;" : "");
		
		response.Headers.Append("X-DHT", "1");
		await response.WriteTextAsync(MediaTypeNames.Text.JavaScript, script, cancellationToken);
	}
}
