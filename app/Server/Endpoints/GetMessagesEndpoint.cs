using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DHT.Server.Data.Filters;
using DHT.Server.Database;
using DHT.Utils.Http;
using Microsoft.AspNetCore.Http;
using GetMessagesJsonContext = DHT.Server.Endpoints.Responses.GetMessagesJsonContext;

namespace DHT.Server.Endpoints;

sealed class GetMessagesEndpoint : BaseEndpoint {
	public GetMessagesEndpoint(IDatabaseFile db) : base(db) {}

	protected override Task<IHttpOutput> Respond(HttpContext ctx) {
		HashSet<ulong> messageIdSet;
		try {
			var messageIds = ctx.Request.Query["id"];
			messageIdSet = messageIds.Select(ulong.Parse!).ToHashSet();
		} catch (Exception) {
			throw new HttpException(HttpStatusCode.BadRequest, "Invalid message ids.");
		}

		var messageFilter = new MessageFilter {
			MessageIds = messageIdSet
		};
		
		var messages = Db.GetMessages(messageFilter).ToDictionary(static message => message.Id, static message => message.Text);
		var response = new HttpOutput.Json<Dictionary<ulong, string>>(messages, GetMessagesJsonContext.Default.DictionaryUInt64String);
		return Task.FromResult<IHttpOutput>(response);
	}
}
