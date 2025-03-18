import discord from "./discord.mjs";
import gui from "./gui.mjs";
import state from "./state.mjs";
import "./polyfills.mjs";
import servers from "./servers.mjs";

window.DISCORD = discord;

document.addEventListener("DOMContentLoaded", () => {
	discord.setup();
	gui.setup();
	
	gui.onOptionMessagesPerPageChanged(() => {
		state.setMessagesPerPage(gui.getOptionMessagesPerPage());
	});
	
	state.setMessagesPerPage(gui.getOptionMessagesPerPage());
	
	gui.onOptMessageFilterChanged(filter => {
		state.setActiveFilter(filter);
	});
	
	gui.onNavigationButtonClicked(action => {
		state.updateCurrentPage(action);
	});
	
	state.onUsersRefreshed(users => {
		gui.updateUserList(users);
		servers.update()
	});
	
	state.onChannelsRefreshed((channels, selected) => {
		gui.updateChannelList(channels, selected, state.selectChannel);
		servers.update()
	});
	
	state.onMessagesRefreshed(messages => {
		gui.updateNavigation(state.getCurrentPage(), state.getPageCount());
		gui.updateMessageList(messages);
		gui.scrollMessagesToTop();
	});
	
	async function fetchUrl(path, contentType) {
		const response = await fetch("/" + path + "?token=" + encodeURIComponent(window.DHT_SERVER_TOKEN) + "&session=" + encodeURIComponent(window.DHT_SERVER_SESSION), {
			method: "GET",
			headers: {
				"Content-Type": contentType,
			},
			credentials: "omit",
			redirect: "error",
		});
		
		if (!response.ok) {
			throw "Unexpected response status: " + response.statusText;
		}
		
		return response;
	}
	
	async function processLines(response, callback) {
		let body = "";
		
		for await (const chunk of response.body.pipeThrough(new TextDecoderStream("utf-8"))) {
			body += chunk;
			
			let startIndex = 0;
			
			while (true) {
				const endIndex = body.indexOf("\n", startIndex);
				if (endIndex === -1) {
					break;
				}
				
				callback(body.substring(startIndex, endIndex));
				startIndex = endIndex + 1;
			}
			
			body = body.substring(startIndex);
		}
		
		if (body !== "") {
			callback(body);
		}
	}
	
	async function loadData() {
		try {
			const metadataResponse = await fetchUrl("get-viewer-metadata", "application/json");
			const metadataJson = await metadataResponse.json();
			
			const messagesResponse = await fetchUrl("get-viewer-messages", "application/x-ndjson");
			const messages = {};
			
			await processLines(messagesResponse, line => {
				const message = JSON.parse(line);
				const channel = message.c;
				
				const channelMessages = messages[channel] || (messages[channel] = {});
				channelMessages[message.id] = message;
				
				delete message.id;
				delete message.c;
			});
			
			state.uploadFile(metadataJson, messages);
		} catch (e) {
			console.error(e);
			alert("Could not load data, see console for details.");
			document.querySelector("#channels > div.loading").remove();
		}
	}
	
	loadData();
});
