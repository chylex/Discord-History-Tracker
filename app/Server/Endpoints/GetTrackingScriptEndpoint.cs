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
	private readonly ServerParameters serverParameters;
	private readonly ResourceLoader resources;

	public GetTrackingScriptEndpoint(IDatabaseFile db, ServerParameters parameters, ResourceLoader resources) : base(db) {
		this.serverParameters = parameters;
		this.resources = resources;
	}

	protected override async Task<IHttpOutput> Respond(HttpContext ctx) {
		string bootstrap = await resources.ReadTextAsync("Tracker/bootstrap.js");
		string script = bootstrap.Replace("= 0; /*[PORT]*/", "= " + serverParameters.Port + ";")
		                         .Replace("/*[TOKEN]*/", HttpUtility.JavaScriptStringEncode(serverParameters.Token))
		                         .Replace("/*[IMPORTS]*/", await resources.ReadJoinedAsync("Tracker/scripts/", '\n'))
		                         .Replace("/*[CSS-CONTROLLER]*/", await resources.ReadTextAsync("Tracker/styles/controller.css"))
		                         .Replace("/*[CSS-SETTINGS]*/", await resources.ReadTextAsync("Tracker/styles/settings.css"))
		                         .Replace("/*[DEBUGGER]*/", ctx.Request.Query.ContainsKey("debug") ? "debugger;" : "");
		
		ctx.Response.Headers.Append("X-DHT", "1");
		return new HttpOutput.File("text/javascript", Encoding.UTF8.GetBytes(script));
	}
}
