using System.Threading.Tasks;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;

namespace DHT.Server.Endpoints;

sealed class ViewerEndpoint : BaseEndpoint {
	private readonly ServerParameters serverParameters;
	
	public ViewerEndpoint(IDatabaseFile db, ServerParameters parameters) : base(db) {
		serverParameters = parameters;
	}
	
	protected override Task<IHttpOutput> Respond(HttpContext ctx) {
		string path = (string) ctx.Request.RouteValues["url"]!;
		return null;
	}
}
