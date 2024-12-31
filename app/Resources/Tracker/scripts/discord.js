// noinspection JSUnresolvedVariable
// noinspection LocalVariableNamingConventionJS
class DISCORD {
	
	// https://discord.com/developers/docs/resources/channel#channel-object-channel-types
	static CHANNEL_TYPE = {
		GUILD_TEXT: 0,
		DM: 1,
		GROUP_DM: 3,
		GUILD_ANNOUNCEMENT: 5,
		ANNOUNCEMENT_THREAD: 10,
		PUBLIC_THREAD: 11,
		PRIVATE_THREAD: 12,
		
		isPrivate(type) {
			return type === this.DM
				|| type === this.GROUP_DM;
		},
		
		isNavigableGuildChannel(type) {
			return type === this.GUILD_TEXT
				|| type === this.GUILD_ANNOUNCEMENT
				|| type === this.ANNOUNCEMENT_THREAD
				|| type === this.PUBLIC_THREAD
				|| type === this.PRIVATE_THREAD;
		}
	};
	
	// https://discord.com/developers/docs/resources/channel#message-object-message-types
	static MESSAGE_TYPE = {
		DEFAULT: 0,
		REPLY: 19,
		THREAD_STARTER: 21
	};
	
	// https://discord.com/developers/docs/topics/permissions#permissions-bitwise-permission-flags
	static PERMISSION = {
		VIEW_CHANNEL: 1n << 10n
	};
	
	/**
	 * @type {Object}
	 * @property {function(String): ?DiscordGuild} getGuild
	 */
	static #guildStore = WEBPACK.findModule("guildStore", WEBPACK.filterByProps("getGuild", "getGuilds", "getGuildIds"));
	
	/**
	 * @type {Object}
	 * @property {function(String): Boolean} isOptInEnabled
	 * @property {function(String): Set<String>} getOptedInChannels
	 */
	static #guildSettings = WEBPACK.findModule("guildSettings", WEBPACK.filterByProps("isOptInEnabled", "getOptedInChannels"));
	
	/**
	 * @type {Object}
	 * @property {function(String): ?DiscordChannel} getChannel
	 * @property {function(String): Array<DiscordChannel>} getMutableGuildChannelsForGuild
	 * @property {function(): Array<DiscordChannel>} getSortedPrivateChannels
	 */
	static #channelStore = WEBPACK.findModule("channelStore", WEBPACK.filterByProps("getChannel", "getMutableGuildChannelsForGuild", "getSortedPrivateChannels"));
	
	/**
	 * @type {function(BigInt, Object): Boolean}
	 */
	static #hasPermission = WEBPACK.findFunction("can", [ "getGuildPermissions", "getChannelPermissions" ]);
	
	/**
	 * @type {function(String): MessageData}
	 */
	static #getMessages = WEBPACK.findFunction("getMessages");
	
	/**
	 * @type {function(String): void}
	 */
	static #jumpToMessage = WEBPACK.findFunction("jumpToMessage");
	
	/**
	 * @type {function(): String}
	 */
	static #getCurrentlySelectedChannelId = WEBPACK.findFunction("getCurrentlySelectedChannelId");
	
	/**
	 * @type {function(String): void}
	 */
	static #selectPrivateChannel = WEBPACK.findFunction("selectPrivateChannel", [ "selectChannel" ]);
	
	/**
	 * @type {function(String, Object, String=null): void}
	 */
	static #transitionToGuildSync = WEBPACK.findFunction("transitionToGuildSync");
	
	static isCompatible() {
		return !!this.#guildStore
			&& !!this.#guildSettings
			&& !!this.#channelStore
			&& !!this.#hasPermission
			&& !!this.#getMessages
			&& !!this.#jumpToMessage
			&& !!this.#getCurrentlySelectedChannelId
			&& !!this.#selectPrivateChannel
			&& !!this.#transitionToGuildSync;
	}
	
	static getMessageOuterElement() {
		return DOM.queryReactClass("messagesWrapper");
	}
	
	static getMessageScrollerElement() {
		return DOM.queryReactClass("scroller", this.getMessageOuterElement());
	}
	
	static loadOlderMessages() {
		const view = this.getMessageScrollerElement();
		
		if (view.scrollTop > 0) {
			view.scrollTop = 0;
		}
	}
	
	static getMessagesFromSelectedChannel() {
		const channelId = this.#getCurrentlySelectedChannelId();
		return channelId ? this.#getMessages(channelId) : null;
	}
	
	/**
	 * Calls the provided function with a list of messages whenever the currently loaded messages change.
	 * @param callback {function(server: ?DiscordGuild, channel: DiscordChannel, messages: Array<DiscordMessage>, hasMoreBefore: boolean)}
	 */
	static setupMessageCallback(callback) {
		const previousMessages = new Set();
		
		const onMessageElementsChanged = force => {
			const messages = this.getMessagesFromSelectedChannel();
			if (!messages || !messages.ready || messages.loadingMore) {
				return false;
			}
			
			const channel = this.#channelStore.getChannel(messages.channelId);
			if (!channel) {
				return false;
			}
			
			const hasChanged = force || !messages.hasMoreBefore || messages.some(message => !previousMessages.has(message.id));
			if (!hasChanged) {
				return false;
			}
			
			previousMessages.clear();
			for (const message of messages._array) {
				previousMessages.add(message.id);
			}
			
			const server = this.#guildStore.getGuild(channel.guild_id);
			
			callback(server, channel, messages._array, messages.hasMoreBefore);
			return true;
		};
		
		let debounceTimer;
		
		/**
		 * Do not trigger the callback too often due to autoscrolling.
		 */
		const onMessageElementsChangedLater = function() {
			window.clearTimeout(debounceTimer);
			debounceTimer = window.setTimeout(onMessageElementsChanged, 100);
		};
		
		const observer = new MutationObserver(function() {
			onMessageElementsChangedLater();
		});
		
		let skipsLeft = 0;
		let observedElement = null;
		
		const observerTimer = window.setInterval(() => {
			if (skipsLeft > 0) {
				--skipsLeft;
				return;
			}
			
			const view = this.getMessageOuterElement();
			
			if (!view) {
				skipsLeft = 1;
				return;
			}
			
			if (observedElement !== null && observedElement.isConnected) {
				return;
			}
			
			observedElement = view.querySelector("[data-list-id='chat-messages']");
			
			if (observedElement) {
				console.debug("[DHT] Observed message container.");
				observer.observe(observedElement, { childList: true });
				onMessageElementsChangedLater();
			}
		}, 400);
		
		window.DHT_ON_UNLOAD.push(() => {
			observer.disconnect();
			observedElement = null;
			window.clearInterval(observerTimer);
		});
		
		return () => onMessageElementsChanged(true);
	}
	
	/**
	 * Selects the next text channel and returns true, otherwise returns false if there are no more channels.
	 */
	static selectNextTextChannel() {
		const currentChannel = this.#channelStore.getChannel(this.#getCurrentlySelectedChannelId());
		if (!currentChannel) {
			return false;
		}
		
		if (this.CHANNEL_TYPE.isPrivate(currentChannel.type)) {
			const privateChannel = this.#channelStore.getSortedPrivateChannels();
			const currentIndex = privateChannel.findIndex(channel => channel.id === currentChannel.id);
			
			if (currentIndex === -1 || currentIndex === privateChannel.length - 1) {
				return false;
			}
			
			this.#selectPrivateChannel(privateChannel[currentIndex + 1].id);
			return true;
		}
		else {
			const guildId = currentChannel.guild_id;
			
			let isChannelOptedIn;
			if (this.#guildSettings.isOptInEnabled(guildId)) {
				const optedInChannels = this.#guildSettings.getOptedInChannels(guildId);
				isChannelOptedIn = channel => optedInChannels.has(channel.id);
			}
			else {
				isChannelOptedIn = _ => true;
			}
			
			const guildChannelMap = this.#channelStore.getMutableGuildChannelsForGuild(guildId);
			const guildChannels = Object.values(guildChannelMap)
				.filter(channel => this.CHANNEL_TYPE.isNavigableGuildChannel(channel.type) && isChannelOptedIn(channel) && this.#hasPermission(this.PERMISSION.VIEW_CHANNEL, channel))
				.sort((a, b) => a.position - b.position);
			
			const currentIndex = guildChannels.findIndex(channel => channel.id === currentChannel.id);
			
			if (currentIndex === -1 || currentIndex === guildChannels.length - 1) {
				return false;
			}
			
			this.#transitionToGuildSync(guildId, {}, guildChannels[currentIndex + 1].id);
			return true;
		}
	}
}
