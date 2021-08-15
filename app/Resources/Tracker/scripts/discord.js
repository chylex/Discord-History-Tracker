// noinspection JSUnresolvedVariable
class DISCORD {
	static getMessageOuterElement() {
		return DOM.queryReactClass("messagesWrapper");
	}
	
	static getMessageScrollerElement() {
		return DOM.queryReactClass("scroller", this.getMessageOuterElement());
	}
	
	static getMessageElements() {
		return this.getMessageOuterElement().querySelectorAll("[class*='message-']");
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
	 * Returns a setInterval handle.
	 */
	static setupMessageCallback(callback) {
		let skipsLeft = 0;
		let waitForCleanup = false;
		const previousMessages = new Set();
		
		return window.setInterval(() => {
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
			let hasChanged = false;
			
			for (const message of messages) {
				if (!previousMessages.has(message.id)) {
					hasChanged = true;
					break;
				}
			}
			
			if (!hasChanged) {
				return;
			}
			
			previousMessages.clear();
			for (const message of messages) {
				previousMessages.add(message.id);
			}
			
			callback(messages);
		}, 200);
	}
	
	/**
	 * Returns the property object of a message element.
	 * @returns { null | { message: DiscordMessage, channel: Object } }
	 */
	static getMessageElementProps(ele) {
		const props = DOM.getReactProps(ele);
		
		if (props.children && props.children.length >= 4) {
			const childProps = props.children[3].props;
			
			if ("message" in childProps && "channel" in childProps) {
				return childProps;
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
				const props = this.getMessageElementProps(ele);
				
				if (props != null) {
					messages.push(props.message);
				}
			}
			
			return messages;
		} catch (e) {
			console.error(e);
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
			
			for (const ele of this.getMessageElements()) {
				const props = this.getMessageElementProps(ele);
				
				if (props != null) {
					obj = props.channel;
					break;
				}
			}
			
			if (!obj) {
				return null;
			}
			
			const dms = DOM.queryReactClass("privateChannels");
			
			if (dms) {
				let name;
				
				for (const ele of dms.querySelectorAll("[class*='channel-'][class*='selected-'] [class^='name-'] *")) {
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
				const server = {
					"id": obj.guild_id,
					"name": document.querySelector("nav header > h1").innerText,
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
			console.error(e);
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
			const nextChannel = currentChannel && currentChannel.nextElementSibling;
			
			if (!nextChannel || !nextChannel.getAttribute("class").includes("channel-") || !("href" in nextChannel) || !nextChannel.href.includes("/@me/")) {
				return false;
			}
			else {
				nextChannel.click();
				nextChannel.scrollIntoView(true);
				return true;
			}
		}
		else {
			const channelIconNormal = "M5.88657 21C5.57547 21 5.3399 20.7189 5.39427 20.4126L6.00001 17H2.59511C2.28449 17 2.04905 16.7198 2.10259 16.4138L2.27759 15.4138C2.31946 15.1746 2.52722 15 2.77011 15H6.35001L7.41001 9H4.00511C3.69449 9 3.45905 8.71977 3.51259 8.41381L3.68759 7.41381C3.72946 7.17456 3.93722 7 4.18011 7H7.76001L8.39677 3.41262C8.43914 3.17391 8.64664 3 8.88907 3H9.87344C10.1845 3 10.4201 3.28107 10.3657 3.58738L9.76001 7H15.76L16.3968 3.41262C16.4391 3.17391 16.6466 3 16.8891 3H17.8734C18.1845 3 18.4201 3.28107 18.3657 3.58738L17.76 7H21.1649C21.4755 7 21.711 7.28023 21.6574 7.58619L21.4824 8.58619C21.4406 8.82544 21.2328 9 20.9899 9H17.41L16.35 15H19.7549C20.0655 15 20.301 15.2802 20.2474 15.5862L20.0724 16.5862C20.0306 16.8254 19.8228 17 19.5799 17H16L15.3632 20.5874C15.3209 20.8261 15.1134 21 14.8709 21H13.8866C13.5755 21 13.3399 20.7189 13.3943 20.4126L14 17H8.00001L7.36325 20.5874C7.32088 20.8261 7.11337 21 6.87094 21H5.88657ZM9.41045 9L8.35045 15H14.3504L15.4104 9H9.41045Z";
			const channelIconSpecial = "M14 8C14 7.44772 13.5523 7 13 7H9.76001L10.3657 3.58738C10.4201 3.28107 10.1845 3 9.87344 3H8.88907C8.64664 3 8.43914 3.17391 8.39677 3.41262L7.76001 7H4.18011C3.93722 7 3.72946 7.17456 3.68759 7.41381L3.51259 8.41381C3.45905 8.71977 3.69449 9 4.00511 9H7.41001L6.35001 15H2.77011C2.52722 15 2.31946 15.1746 2.27759 15.4138L2.10259 16.4138C2.04905 16.7198 2.28449 17 2.59511 17H6.00001L5.39427 20.4126C5.3399 20.7189 5.57547 21 5.88657 21H6.87094C7.11337 21 7.32088 20.8261 7.36325 20.5874L8.00001 17H14L13.3943 20.4126C13.3399 20.7189 13.5755 21 13.8866 21H14.8709C15.1134 21 15.3209 20.8261 15.3632 20.5874L16 17H19.5799C19.8228 17 20.0306 16.8254 20.0724 16.5862L20.2474 15.5862C20.301 15.2802 20.0655 15 19.7549 15H16.35L16.6758 13.1558C16.7823 12.5529 16.3186 12 15.7063 12C15.2286 12 14.8199 12.3429 14.7368 12.8133L14.3504 15H8.35045L9.41045 9H13C13.5523 9 14 8.55228 14 8Z";
			
			const isValidChannelClass = cls => cls.includes("wrapper-") && !cls.includes("clickable-");
			const isValidChannelType = ele => !!ele.querySelector("path[d=\"" + channelIconNormal + "\"]") || !!ele.querySelector("path[d=\"" + channelIconSpecial + "\"]");
			const isValidChannel = ele => ele.childElementCount > 0 && isValidChannelClass(ele.children[0].className) && isValidChannelType(ele);
			
			const channelListEle = document.querySelector("div[class*='sidebar'] > nav[class*='container'] > div[class*='scroller']");
			
			if (!channelListEle) {
				return false;
			}
			
			const allChannels = Array.prototype.filter.call(channelListEle.querySelectorAll("[class*='containerDefault']"), isValidChannel);
			let nextChannel = null;
			
			for (let index = 0; index < allChannels.length - 1; index++) {
				if (allChannels[index].children[0].className.includes("modeSelected")) {
					nextChannel = allChannels[index + 1];
					break;
				}
			}
			
			if (nextChannel === null) {
				return false;
			}
			
			const nextChannelLink = nextChannel.querySelector("a[href^='/channels/']");
			if (!nextChannelLink) {
				return false;
			}
			
			nextChannelLink.click();
			nextChannel.scrollIntoView(true);
			return true;
		}
	}
}
