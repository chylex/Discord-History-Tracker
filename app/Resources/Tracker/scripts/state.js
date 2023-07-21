// noinspection FunctionWithInconsistentReturnsJS
const STATE = (function() {
	let serverPort = -1;
	let serverToken = "";
	
	const post = function(endpoint, json) {
		const aborter = new AbortController();
		const timeout = window.setTimeout(() => aborter.abort(), 5000);
		
		return new Promise(async (resolve, reject) => {
			let r;
			try {
				r = await fetch("http://127.0.0.1:" + serverPort + endpoint, {
					method: "POST",
					headers: {
						"Content-Type": "application/json",
						"X-DHT-Token": serverToken
					},
					credentials: "omit",
					redirect: "error",
					body: JSON.stringify(json),
					signal: aborter.signal
				});
			} catch (e) {
				if (e.name === "AbortError") {
					reject({ status: "DISCONNECTED" });
					return;
				}
				else {
					reject({ status: "ERROR", message: e.message });
					return;
				}
			} finally {
				window.clearTimeout(timeout);
			}
			
			if (r.status === 200) {
				resolve(r);
				return;
			}
			
			try {
				const message = await r.text();
				reject({ status: "ERROR", message });
			} catch (e) {
				reject({ status: "ERROR", message: e.message });
			}
		});
	};
	
	const trackingStateChangedListeners = [];
	let isTracking = false;
	
	const addedChannels = new Set();
	const addedUsers = new Set();
	
	/**
	 * @name DiscordUser
	 * @property {String} id
	 * @property {String} username
	 * @property {String} discriminator
	 * @property {String} [avatar]
	 * @property {Boolean} [bot]
	 */
	
	/**
	 * @name DiscordMessage
	 * @property {String} id
	 * @property {String} channel_id
	 * @property {DiscordUser} author
	 * @property {String} content
	 * @property {Timestamp} timestamp
	 * @property {Timestamp|null} editedTimestamp
	 * @property {DiscordAttachment[]} attachments
	 * @property {Object[]} embeds
	 * @property {DiscordMessageReaction[]} [reactions]
	 * @property {DiscordMessageReference} [messageReference]
	 * @property {Number} type
	 * @property {String} state
	 */
	
	/**
	 * @name DiscordAttachment
	 * @property {String} id
	 * @property {String} filename
	 * @property {String} [content_type]
	 * @property {String} size
	 * @property {String} url
	 */
	
	/**
	 * @name DiscordMessageReaction
	 * @property {DiscordEmoji} emoji
	 * @property {Number} count
	 */
	
	/**
	 * @name DiscordMessageReference
	 * @property {String} [message_id]
	 */
	
	/**
	 * @name DiscordEmoji
	 * @property {String|null} id
	 * @property {String|null} name
	 * @property {Boolean} animated
	 */
	
	/**
	 * @name Timestamp
	 * @property {Function} toDate
	 */
	
	return {
		setup(port, token) {
			serverPort = port;
			serverToken = token;
		},
		
		onTrackingStateChanged(callback) {
			trackingStateChangedListeners.push(callback);
			callback(isTracking);
		},
		
		isTracking() {
			return isTracking;
		},
		
		setIsTracking(state) {
			if (isTracking !== state) {
				isTracking = state;
				
				if (isTracking) {
					addedChannels.clear();
					addedUsers.clear();
				}
				
				for (const callback of trackingStateChangedListeners) {
					callback(isTracking);
				}
			}
		},
		
		async addDiscordChannel(serverInfo, channelInfo) {
			if (addedChannels.has(channelInfo.id)) {
				return;
			}
			
			const server = {
				id: serverInfo.id,
				name: serverInfo.name,
				type: serverInfo.type
			};
			
			const channel = {
				id: channelInfo.id,
				name: channelInfo.name
			};
			
			if ("extra" in channelInfo) {
				const extra = channelInfo.extra;
				
				if ("parent" in extra) {
					channel.parent = extra.parent;
				}
				
				channel.position = extra.position;
				channel.topic = extra.topic;
				channel.nsfw = extra.nsfw;
			}
			
			await post("/track-channel", { server, channel });
			addedChannels.add(channelInfo.id);
		},
		
		/**
		 * @param {String} channelId
		 * @param {DiscordMessage[]} discordMessageArray
		 */
		async addDiscordMessages(channelId, discordMessageArray) {
			// https://discord.com/developers/docs/resources/channel#message-object-message-types
			discordMessageArray = discordMessageArray.filter(msg => msg.state === "SENT");
			
			if (discordMessageArray.length === 0) {
				return false;
			}
			
			const response = await post("/track-messages", JSON.stringify(discordMessageArray));
			const anyNewMessages = await response.text();
			return anyNewMessages === "1";
		}
	};
})();
