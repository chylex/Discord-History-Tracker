(function() {
	const url = window.location.href;
	
	if (!url.includes("discord.com/") && !url.includes("discordapp.com/") && !confirm("Could not detect Discord in the URL, do you want to run the script anyway?")) {
		return;
	}
	
	if (window.DHT_LOADED) {
		alert("Discord History Tracker is already loaded.");
		return;
	}
	
	window.DHT_LOADED = true;
	window.DHT_ON_UNLOAD = [];
	
	/*[IMPORTS]*/
	
	const port = 0; /*[PORT]*/
	const token = "/*[TOKEN]*/";
	STATE.setup(port, token);
	
	let delayedStopRequests = 0;
	const stopTrackingDelayed = function(callback) {
		delayedStopRequests++;
		
		window.setTimeout(() => {
			STATE.setIsTracking(false);
			delayedStopRequests--;
			
			if (callback) {
				callback();
			}
		}, 200); // give the user visual feedback after clicking the button before switching off
	};
	
	let hasJustStarted = false;
	let isSending = false;
	
	const onError = function(e) {
		console.log(e);
		GUI.setStatus(e.status === "DISCONNECTED" ? "Disconnected" : "Error");
		stopTrackingDelayed(() => isSending = false);
	};
	
	const isNoAction = function(action) {
		return action === null || action === CONSTANTS.AUTOSCROLL_ACTION_NOTHING;
	};
	
	const onTrackingContinued = function(anyNewMessages) {
		if (!STATE.isTracking()) {
			return;
		}
		
		GUI.setStatus("Tracking");
		
		if (hasJustStarted) {
			anyNewMessages = true;
			hasJustStarted = false;
		}
		
		isSending = false;
		
		if (SETTINGS.autoscroll) {
			let action = null;
			
			if (!DISCORD.hasMoreMessages()) {
				action = SETTINGS.afterFirstMsg;
			}
			if (isNoAction(action) && !anyNewMessages) {
				action = SETTINGS.afterSavedMsg;
			}
			
			if (isNoAction(action)) {
				DISCORD.loadOlderMessages();
			}
			else if (action === CONSTANTS.AUTOSCROLL_ACTION_PAUSE || (action === CONSTANTS.AUTOSCROLL_ACTION_SWITCH && !DISCORD.selectNextTextChannel())) {
				GUI.setStatus("Reached End");
				STATE.setIsTracking(false);
			}
		}
	};
	
	let waitUntilSendingFinishedTimer = null;
	
	const onMessagesUpdated = async messages => {
		if (!STATE.isTracking() || delayedStopRequests > 0) {
			return;
		}
		
		if (isSending) {
			window.clearTimeout(waitUntilSendingFinishedTimer);
			
			waitUntilSendingFinishedTimer = window.setTimeout(() => {
				waitUntilSendingFinishedTimer = null;
				onMessagesUpdated(messages);
			}, 100);
			
			return;
		}
		
		const info = DISCORD.getSelectedChannel();
		
		if (!info) {
			GUI.setStatus("Error (Unknown Channel)");
			stopTrackingDelayed();
			return;
		}
		
		isSending = true;
		
		try {
			await STATE.addDiscordChannel(info.server, info.channel);
		} catch (e) {
			onError(e);
			return;
		}
		
		try {
			if (!messages.length) {
				isSending = false;
				onTrackingContinued(false);
			}
			else {
				const anyNewMessages = await STATE.addDiscordMessages(info.id, messages);
				onTrackingContinued(anyNewMessages);
			}
		} catch (e) {
			onError(e);
		}
	};
	
	DISCORD.setupMessageCallback(onMessagesUpdated);
	
	STATE.onTrackingStateChanged(enabled => {
		if (enabled) {
			const messages = DISCORD.getMessages();
			
			if (messages.length === 0) {
				stopTrackingDelayed(() => alert("Cannot see any messages."));
				return;
			}
			
			GUI.setStatus("Starting");
			hasJustStarted = true;
			// noinspection JSIgnoredPromiseFromCall
			onMessagesUpdated(messages);
		}
		else {
			isSending = false;
		}
	});
	
	GUI.showController();
	
	if (IS_FIRST_RUN) {
		GUI.showSettings();
	}
})();
