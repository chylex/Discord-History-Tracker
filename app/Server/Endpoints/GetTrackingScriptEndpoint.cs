using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Http;
using DHT.Utils.Resources;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class GetTrackingScriptEndpoint : BaseEndpoint {
	private static ResourceLoader Resources { get; } = new (Assembly.GetExecutingAssembly());
	
	private readonly ServerParameters serverParameters;
	
	public GetTrackingScriptEndpoint(IDatabaseFile db, ServerParameters parameters) : base(db) {
		serverParameters = parameters;
	}

	protected override async Task<IHttpOutput> Respond(HttpContext ctx) {
		string bootstrap = await Resources.ReadTextAsync("Tracker/bootstrap.js");
		string script = bootstrap.Replace("= 0; /*[PORT]*/", "= " + serverParameters.Port + ";")
		                         .Replace("/*[TOKEN]*/", HttpUtility.JavaScriptStringEncode(serverParameters.Token))
		                         .Replace("/*[IMPORTS]*/", await Resources.ReadJoinedAsync("Tracker/scripts/", '\n'))
		                         .Replace("/*[CSS-CONTROLLER]*/", await Resources.ReadTextAsync("Tracker/styles/controller.css"))
		                         .Replace("/*[CSS-SETTINGS]*/", await Resources.ReadTextAsync("Tracker/styles/settings.css"))
		                         .Replace("/*[DEBUGGER]*/", ctx.Request.Query.ContainsKey("debug") ? "debugger;" : "");
		
		ctx.Response.Headers.Append("X-DHT", "1");
		return new HttpOutput.File("text/javascript", Encoding.UTF8.GetBytes(script));
	}
}
