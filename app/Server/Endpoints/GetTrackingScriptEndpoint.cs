using System.Net.Mime;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Http;
using DHT.Utils.Resources;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class GetTrackingScriptEndpoint(IDatabaseFile db, ServerParameters parameters) : BaseEndpoint(db) {
	private static ResourceLoader Resources { get; } = new (Assembly.GetExecutingAssembly());

	protected override async Task Respond(HttpRequest request, HttpResponse response) {
		string bootstrap = await Resources.ReadTextAsync("Tracker/bootstrap.js");
		string script = bootstrap.Replace("= 0; /*[PORT]*/", "= " + parameters.Port + ";")
		                         .Replace("/*[TOKEN]*/", HttpUtility.JavaScriptStringEncode(parameters.Token))
		                         .Replace("/*[IMPORTS]*/", await Resources.ReadJoinedAsync("Tracker/scripts/", '\n'))
		                         .Replace("/*[CSS-CONTROLLER]*/", await Resources.ReadTextAsync("Tracker/styles/controller.css"))
		                         .Replace("/*[CSS-SETTINGS]*/", await Resources.ReadTextAsync("Tracker/styles/settings.css"))
		                         .Replace("/*[DEBUGGER]*/", request.Query.ContainsKey("debug") ? "debugger;" : "");
		
		response.Headers.Append("X-DHT", "1");
		await response.WriteTextAsync(MediaTypeNames.Text.JavaScript, script);
	}
}
