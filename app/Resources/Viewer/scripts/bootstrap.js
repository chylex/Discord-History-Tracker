document.addEventListener("DOMContentLoaded", () => {
	DISCORD.setup();
	GUI.setup();
	
	GUI.onOptionMessagesPerPageChanged(() => {
		STATE.setMessagesPerPage(GUI.getOptionMessagesPerPage());
	});
	
	STATE.setMessagesPerPage(GUI.getOptionMessagesPerPage());
	
	GUI.onOptMessageFilterChanged(filter => {
		STATE.setActiveFilter(filter);
	});
	
	GUI.onNavigationButtonClicked(action => {
		STATE.updateCurrentPage(action);
	});
	
	STATE.onUsersRefreshed(users => {
		GUI.updateUserList(users);
	});
	
	STATE.onChannelsRefreshed((channels, selected) => {
		GUI.updateChannelList(channels, selected, STATE.selectChannel);
	});
	
	STATE.onMessagesRefreshed(messages => {
		GUI.updateNavigation(STATE.getCurrentPage(), STATE.getPageCount());
		GUI.updateMessageList(messages);
		GUI.scrollMessagesToTop();
	});
	
	try {
		STATE.uploadFile(JSON.parse(window.DHT_EMBEDDED));
	} catch (e) {
		console.error(e);
		alert("Could not parse embedded file, see console for details.");
	}
});
