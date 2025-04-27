using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using DHT.Server.Service;
using DHT.Utils.Resources;
using Sisk.Core.Http;

namespace DHT.Server.Endpoints;

sealed class GetTrackingScriptEndpoint(ServerParameters parameters, ResourceLoader resources) : BaseEndpoint {
	protected override async Task<HttpResponse> Respond(HttpRequest request) {
		string bootstrap = await resources.ReadTextAsync("Tracker/bootstrap.js");
		string script = bootstrap.Replace("= 0; /*[PORT]*/", "= " + parameters.Port + ";")
		                         .Replace("/*[TOKEN]*/", HttpUtility.JavaScriptStringEncode(parameters.Token))
		                         .Replace("/*[IMPORTS]*/", await resources.ReadJoinedAsync("Tracker/scripts/", separator: '\n', [ "/webpack.js" ]))
		                         .Replace("/*[CSS-CONTROLLER]*/", await resources.ReadTextAsync("Tracker/styles/controller.css"))
		                         .Replace("/*[CSS-SETTINGS]*/", await resources.ReadTextAsync("Tracker/styles/settings.css"))
		                         .Replace("/*[DEBUGGER]*/", request.Query.ContainsKey("debug") ? "debugger;" : "");
		
		return new HttpResponse()
		       .WithHeader("X-DHT", "1")
		       .WithContent(script, Encoding.UTF8, MediaTypeNames.Text.JavaScript);
	}
}
