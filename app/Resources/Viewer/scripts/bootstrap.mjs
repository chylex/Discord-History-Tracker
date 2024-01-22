import discord from "./discord.mjs";
import gui from "./gui.mjs";
import state from "./state.mjs";

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
	});
	
	state.onChannelsRefreshed((channels, selected) => {
		gui.updateChannelList(channels, selected, state.selectChannel);
	});
	
	state.onMessagesRefreshed(messages => {
		gui.updateNavigation(state.getCurrentPage(), state.getPageCount());
		gui.updateMessageList(messages);
		gui.scrollMessagesToTop();
	});
	
	async function loadData() {
		try {
			const response = await fetch("/get-viewer-data?token=" + encodeURIComponent(window.DHT_SERVER_TOKEN) + "&session=" + encodeURIComponent(window.DHT_SERVER_SESSION), {
				method: "GET",
				headers: {
					"Content-Type": "application/json",
				},
				credentials: "omit",
				redirect: "error",
			});

			state.uploadFile(await response.json());
		} catch (e) {
			console.error(e);
			alert("Could not load data, see console for details.");
			document.querySelector("#channels > div.loading").remove();
		}
	}
	
	loadData();
});
