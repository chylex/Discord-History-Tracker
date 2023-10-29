// noinspection JSUnresolvedVariable
class DISCORD {
	static getMessageOuterElement() {
		return DOM.queryReactClass("messagesWrapper");
	}
	
	static getMessageScrollerElement() {
		return DOM.queryReactClass("scroller", this.getMessageOuterElement());
	}
	
	static getMessageElements() {
		return this.getMessageOuterElement().querySelectorAll("[class*='message_']");
	}
	
	static hasMoreMessages() {
		return document.querySelector("#messagesNavigationDescription + [class^=container]") === null;
	}
	
	static loadOlderMessages() {
		const view = this.getMessageScrollerElement();
		
		if (view.scrollTop > 0) {
			view.scrollTop = 0;
		}
	}
	
	/**
	 * Calls the provided function with a list of messages whenever the currently loaded messages change.
	 */
	static setupMessageCallback(callback) {
		let skipsLeft = 0;
		let waitForCleanup = false;
		const previousMessages = new Set();
		
		const timer = window.setInterval(() => {
			if (skipsLeft > 0) {
				--skipsLeft;
				return;
			}
			
			const view = this.getMessageOuterElement();
			
			if (!view) {
				skipsLeft = 2;
				return;
			}
			
			const anyMessage = DOM.queryReactClass("message", this.getMessageOuterElement());
			const messageCount = anyMessage ? anyMessage.parentElement.children.length : 0;
			
			if (messageCount > 300) {
				if (waitForCleanup) {
					return;
				}
				
				skipsLeft = 3;
				waitForCleanup = true;
				
				window.setTimeout(() => {
					const view = this.getMessageScrollerElement();
					// noinspection JSUnusedGlobalSymbols
					view.scrollTop = view.scrollHeight / 2;
				}, 1);
			}
			else {
				waitForCleanup = false;
			}
			
			const messages = this.getMessages();
			const hasChanged = messages.some(message => !previousMessages.has(message.id)) || !this.hasMoreMessages();
			
			if (!hasChanged) {
				return;
			}
			
			previousMessages.clear();
			for (const message of messages) {
				previousMessages.add(message.id);
			}
			
			callback(messages);
		}, 200);
		
		window.DHT_ON_UNLOAD.push(() => window.clearInterval(timer));
	}
	
	/**
	 * Returns the property object of a message element.
	 * @returns { null | { message: DiscordMessage, channel: Object } }
	 */
	static getMessageElementProps(ele) {
		const props = DOM.getReactProps(ele);
		
		if (props.children && props.children.length) {
			for (let i = 3; i < props.children.length; i++) {
				const childProps = props.children[i].props;
				
				if (childProps && "message" in childProps && "channel" in childProps) {
					return childProps;
				}
			}
		}
		
		return null;
	}
	
	/**
	 * Returns an array containing currently loaded messages.
	 */
	static getMessages() {
		try {
			const messages = [];
			
			for (const ele of this.getMessageElements()) {
				try {
					const props = this.getMessageElementProps(ele);
					
					if (props != null) {
						messages.push(props.message);
					}
				} catch (e) {
					console.error("[DHT] Error extracing message data, skipping it.", e, ele, DOM.tryGetReactProps(ele));
				}
			}
			
			return messages;
		} catch (e) {
			console.error("[DHT] Error retrieving messages.", e);
			return [];
		}
	}
	
	/**
	 * Returns an object containing the selected server and channel information.
	 * For types DM and GROUP, the server and channel ids and names are identical.
	 * @returns { {} | null }
	 */
	static getSelectedChannel() {
		try {
			let obj;
			
			try {
				for (const child of DOM.getReactProps(DOM.queryReactClass("chatContent")).children) {
					if (child && child.props && child.props.channel) {
						obj = child.props.channel;
						break;
					}
				}
			} catch (e) {
				console.error("[DHT] Error retrieving selected channel from 'chatContent' element.", e);
				
				for (const ele of this.getMessageElements()) {
					const props = this.getMessageElementProps(ele);
					
					if (props != null) {
						obj = props.channel;
						break;
					}
				}
			}
			
			if (!obj || typeof obj.id !== "string") {
				return null;
			}
			
			const dms = DOM.queryReactClass("privateChannels");
			
			if (dms) {
				let name;
				
				for (const ele of dms.querySelectorAll("[class*='channel_'] [class*='selected_'] [class^='name_'] *")) {
					const node = Array.prototype.find.call(ele.childNodes, node => node.nodeType === Node.TEXT_NODE);
					
					if (node) {
						name = node.nodeValue;
						break;
					}
				}
				
				if (!name) {
					return null;
				}
				
				let type;
				
				// https://discord.com/developers/docs/resources/channel#channel-object-channel-types
				switch (obj.type) {
					case 1: type = "DM"; break;
					case 3: type = "GROUP"; break;
					default: return null;
				}
				
				const id = obj.id;
				const server = { id, name, type };
				const channel = { id, name };
				
				return { server, channel };
			}
			else if (obj.guild_id) {
				let guild;
				
				for (const child of DOM.getReactProps(document.querySelector("nav header [class*='headerContent_']")).children) {
					if (child && child.props && child.props.guild) {
						guild = child.props.guild;
						break;
					}
				}
				
				if (!guild || typeof guild.name !== "string" || obj.guild_id !== guild.id) {
					return null;
				}
				
				const server = {
					"id": guild.id,
					"name": guild.name,
					"type": "SERVER"
				};
				
				const channel = {
					"id": obj.id,
					"name": obj.name,
					"extra": {
						"nsfw": obj.nsfw
					}
				};
				
				if (obj.parent_id) {
					channel["extra"]["parent"] = obj.parent_id;
				}
				else {
					channel["extra"]["position"] = obj.position;
					channel["extra"]["topic"] = obj.topic;
				}
				
				return { server, channel };
			}
			else {
				return null;
			}
		} catch (e) {
			console.error("[DHT] Error retrieving selected channel.", e);
			return null;
		}
	}
	
	/**
	 * Selects the next text channel and returns true, otherwise returns false if there are no more channels.
	 */
	static selectNextTextChannel() {
		const dms = DOM.queryReactClass("privateChannels");
		
		if (dms) {
			const currentChannel = DOM.queryReactClass("selected", dms);
			const currentChannelContainer = currentChannel && currentChannel.closest("[class*='channel_']");
			const nextChannel = currentChannelContainer && currentChannelContainer.nextElementSibling;
			
			if (!nextChannel || !nextChannel.getAttribute("class").includes("channel_")) {
				return false;
			}
			
			const nextChannelLink = nextChannel.querySelector("a[href*='/@me/']");
			if (!nextChannelLink) {
				return false;
			}
			
			nextChannelLink.click();
			nextChannelLink.scrollIntoView(true);
			return true;
		}
		else {
			const channelListEle = document.getElementById("channels");
			if (!channelListEle) {
				return false;
			}
			
			function getLinkElement(channel) {
				return channel.querySelector("a[href^='/channels/'][role='link']");
			}
			
			const allTextChannels = Array.prototype.filter.call(channelListEle.querySelectorAll("[class*='containerDefault']"), ele => getLinkElement(ele) !== null);
			let nextChannel = null;
			
			for (let index = 0; index < allTextChannels.length - 1; index++) {
				if (allTextChannels[index].className.includes("selected_")) {
					nextChannel = allTextChannels[index + 1];
					break;
				}
			}
			
			if (nextChannel === null) {
				return false;
			}
			
			const nextChannelLink = getLinkElement(nextChannel);
			if (!nextChannelLink) {
				return false;
			}
			
			nextChannelLink.click();
			nextChannel.scrollIntoView(true);
			return true;
		}
	}
}
