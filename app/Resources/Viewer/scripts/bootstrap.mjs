import discord from "./discord";
import gui from "./gui";
import state from "./state";

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
	
	try {
		state.uploadFile(JSON.parse(window.DHT_EMBEDDED));
	} catch (e) {
		console.error(e);
		alert("Could not parse embedded file, see console for details.");
	}
});
