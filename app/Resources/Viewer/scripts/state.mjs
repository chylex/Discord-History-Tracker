import settings from "./settings.mjs";
import processor from "./processor.mjs";

// noinspection FunctionWithInconsistentReturnsJS
export default (function() {
	/**
	 * @type {{}}
	 * @property {{}} users
	 * @property {{}} servers
	 * @property {{}} channels
	 */
	let loadedFileMeta;
	let loadedFileData;
	
	let loadedMessages;
	
	let filterFunction;
	let selectedChannel;
	let currentPage;
	let messagesPerPage;
	
	const getUser = function(id) {
		return loadedFileMeta.users[id] || { "name": "&lt;unknown&gt;" };
	};
	
	const getUserList = function() {
		return loadedFileMeta ? loadedFileMeta.users : [];
	};
	
	const getServer = function(id) {
		return loadedFileMeta.servers[id] || { "name": "&lt;unknown&gt;", "type": "unknown" };
	};
	
	const generateChannelHierarchy = function() {
		/**
		 * @type {Map<string, Set>}
		 */
		const hierarchy = new Map();
		
		if (!loadedFileMeta) {
			return hierarchy;
		}
		
		/**
		 * @returns {Set}
		 */
		function getChildren(parentId) {
			let children = hierarchy.get(parentId);
			
			if (!children) {
				children = new Set();
				hierarchy.set(parentId, children);
			}
			
			return children;
		}
		
		for (const [ id, channel ] of Object.entries(loadedFileMeta.channels)) {
			getChildren(channel.parent || "").add(id);
		}
		
		const unreachableIds = new Set(hierarchy.keys());
		
		function reachIds(parentId) {
			unreachableIds.delete(parentId);
			
			const children = hierarchy.get(parentId);
			
			if (children) {
				for (const id of children) {
					reachIds(id);
				}
			}
		}
		
		reachIds("");
		
		const rootChildren = getChildren("");
		
		for (const unreachableId of unreachableIds) {
			for (const id of hierarchy.get(unreachableId)) {
				rootChildren.add(id);
			}
			
			hierarchy.delete(unreachableId);
		}
		
		return hierarchy;
	};
	
	const generateChannelOrder = function() {
		if (!loadedFileMeta) {
			return {};
		}
		
		const channels = loadedFileMeta.channels;
		const hierarchy = generateChannelHierarchy();
		
		function getSortedSubTree(parentId) {
			const children = hierarchy.get(parentId);
			if (!children) {
				return [];
			}
			
			const sortedChildren = Array.from(children);
			
			sortedChildren.sort((id1, id2) => {
				const c1 = channels[id1];
				const c2 = channels[id2];
				const s1 = getServer(c1.server);
				const s2 = getServer(c2.server);
				
				return s1.type.localeCompare(s2.type, "en") ||
					s1.name.toLocaleLowerCase().localeCompare(s2.name.toLocaleLowerCase(), undefined, { numeric: true }) ||
					(c1.position || -1) - (c2.position || -1) ||
					c1.name.toLocaleLowerCase().localeCompare(c2.name.toLocaleLowerCase(), undefined, { numeric: true });
			});
			
			const subTree = [];
			
			for (const id of sortedChildren) {
				subTree.push(id);
				subTree.push(...getSortedSubTree(id));
			}
			
			return subTree;
		}
		
		const orderArray = getSortedSubTree("");
		const orderMap = {};
		
		for (let i = 0; i < orderArray.length; i++) {
			orderMap[orderArray[i]] = i;
		}
		
		return orderMap;
	};
	
	const getChannelList = function() {
		if (!loadedFileMeta) {
			return [];
		}
		
		const channels = loadedFileMeta.channels;
		const channelOrder = generateChannelOrder();
		
		return Object.keys(channels).map(key => ({
			"id": key,
			"name": channels[key].name,
			"server": getServer(channels[key].server),
			"msgcount": getFilteredMessageKeys(key).length,
			"topic": channels[key].topic || "",
			"nsfw": channels[key].nsfw || false,
		})).sort((ac, bc) => {
			return channelOrder[ac.id] - channelOrder[bc.id];
		});
	};
	
	const getMessages = function(channel) {
		return loadedFileData[channel] || {};
	};
	
	const getMessageById = function(id) {
		for (const messages of Object.values(loadedFileData)) {
			if (id in messages) {
				return messages[id];
			}
		}
		
		return null;
	};
	
	const getMessageChannel = function(id) {
		for (const [ channel, messages ] of Object.entries(loadedFileData)) {
			if (id in messages) {
				return channel;
			}
		}
		
		return null;
	};
	
	const getMessageList = function() {
		if (!loadedMessages) {
			return [];
		}
		
		const messages = getMessages(selectedChannel);
		const startIndex = messagesPerPage * (root.getCurrentPage() - 1);
		
		return loadedMessages.slice(startIndex, !messagesPerPage ? undefined : startIndex + messagesPerPage).map(key => {
			/**
			 * @type {{}}
			 * @property {Number} u
			 * @property {Number} t
			 * @property {String} m
			 * @property {Number} [te]
			 * @property {String} [r]
			 * @property {{}[]} [a]
			 * @property {String[]} [e]
			 * @property {{}[]} [re]
			 */
			const message = messages[key];
			const user = getUser(message.u);
			const avatar = user.avatar ? { id: message.u, path: user.avatar } : null;
			
			const obj = {
				user,
				avatar,
				"timestamp": message.t,
				"jump": key,
			};
			
			if ("m" in message) {
				obj["contents"] = message.m;
			}
			
			if ("e" in message) {
				obj["embeds"] = message.e.map(embed => JSON.parse(embed));
			}
			
			if ("a" in message) {
				obj["attachments"] = message.a;
			}
			
			if ("te" in message) {
				obj["edit"] = message.te;
			}
			
			if ("r" in message) {
				const replyMessage = getMessageById(message.r);
				const replyUser = replyMessage ? getUser(replyMessage.u) : null;
				const replyAvatar = replyUser && replyUser.avatar ? { id: replyMessage.u, path: replyUser.avatar } : null;
				
				obj["reply"] = replyMessage ? {
					"id": message.r,
					"user": replyUser,
					"avatar": replyAvatar,
					"contents": replyMessage.m
				} : null;
			}
			
			if ("re" in message) {
				obj["reactions"] = message.re;
			}
			
			return obj;
		});
	};
	
	let eventOnUsersRefreshed;
	let eventOnChannelsRefreshed;
	let eventOnMessagesRefreshed;
	
	const triggerUsersRefreshed = function() {
		eventOnUsersRefreshed && eventOnUsersRefreshed(getUserList());
	};
	
	const triggerChannelsRefreshed = function(selectedChannel) {
		eventOnChannelsRefreshed && eventOnChannelsRefreshed(getChannelList(), selectedChannel);
	};
	
	const triggerMessagesRefreshed = function() {
		eventOnMessagesRefreshed && eventOnMessagesRefreshed(getMessageList());
	};
	
	const getFilteredMessageKeys = function(channel) {
		const messages = getMessages(channel);
		let keys = Object.keys(messages);
		
		if (filterFunction) {
			keys = keys.filter(key => filterFunction(messages[key]));
		}
		
		return keys;
	};
	
	const root = {
		onChannelsRefreshed(callback) {
			eventOnChannelsRefreshed = callback;
		},
		
		onMessagesRefreshed(callback) {
			eventOnMessagesRefreshed = callback;
		},
		
		onUsersRefreshed(callback) {
			eventOnUsersRefreshed = callback;
		},
		
		uploadFile(meta, data) {
			if (loadedFileMeta != null) {
				throw "A file is already loaded!";
			}
			
			if (typeof meta !== "object" || typeof data !== "object") {
				throw "Invalid file format!";
			}
			
			loadedFileMeta = meta;
			loadedFileData = data;
			loadedMessages = null;
			
			selectedChannel = null;
			currentPage = 1;
			
			triggerUsersRefreshed();
			triggerChannelsRefreshed();
			triggerMessagesRefreshed();
			
			settings.onSettingsChanged(() => triggerMessagesRefreshed());
		},
		
		getChannelName(channel) {
			const channelObj = loadedFileMeta.channels[channel];
			return (channelObj && channelObj.name) || channel;
		},
		
		getUserName(user) {
			const userObj = loadedFileMeta.users[user];
			return (userObj && userObj.name) || user;
		},
		
		getUserDisplayName(user) {
			const userObj = loadedFileMeta.users[user];
			return (userObj && (userObj.displayName || userObj.name)) || user;
		},
		
		selectChannel(channel) {
			currentPage = 1;
			selectedChannel = channel;
			
			loadedMessages = getFilteredMessageKeys(channel).sort(processor.SORTER.oldestToNewest);
			triggerMessagesRefreshed();
		},
		
		setMessagesPerPage(amount) {
			messagesPerPage = amount;
			triggerMessagesRefreshed();
		},
		
		updateCurrentPage(action) {
			switch (action) {
				case "first":
					currentPage = 1;
					break;
				
				case "prev":
					currentPage = Math.max(1, currentPage - 1);
					break;
				
				case "next":
					currentPage = Math.min(this.getPageCount(), currentPage + 1);
					break;
				
				case "last":
					currentPage = this.getPageCount();
					break;
				
				case "pick":
					const page = parseInt(prompt("Select page:", currentPage), 10);
					
					if (!page && page !== 0) {
						return;
					}
					
					currentPage = Math.max(1, Math.min(this.getPageCount(), page));
					break;
			}
			
			triggerMessagesRefreshed();
		},
		
		getCurrentPage() {
			const total = this.getPageCount();
			
			if (currentPage > total && total > 0) {
				currentPage = total;
			}
			
			return currentPage || 1;
		},
		
		getPageCount() {
			return !loadedMessages ? 0 : (!messagesPerPage ? 1 : Math.ceil(loadedMessages.length / messagesPerPage));
		},
		
		navigateToMessage(id) {
			if (!loadedMessages) {
				return -1;
			}
			
			const channel = getMessageChannel(id);
			
			if (channel !== null && channel !== selectedChannel) {
				triggerChannelsRefreshed(channel);
				this.selectChannel(channel);
			}
			
			const index = loadedMessages.indexOf(id);
			
			if (index === -1) {
				return -1;
			}
			
			currentPage = Math.max(1, Math.min(this.getPageCount(), 1 + Math.floor(index / messagesPerPage)));
			triggerMessagesRefreshed();
			return index % messagesPerPage;
		},
		
		setActiveFilter(filter) {
			switch (filter ? filter.type : "") {
				case "user":
					filterFunction = processor.FILTER.byUser(filter.value);
					break;
				
				case "contents":
					filterFunction = processor.FILTER.byContents(filter.value);
					break;
				
				case "withimages":
					filterFunction = processor.FILTER.withImages();
					break;
				
				case "withdownloads":
					filterFunction = processor.FILTER.withDownloads();
					break;
				
				case "edited":
					filterFunction = processor.FILTER.isEdited();
					break;
				
				default:
					filterFunction = null;
					break;
			}
			
			this.hasActiveFilter = filterFunction != null;
			
			triggerChannelsRefreshed(selectedChannel);
			
			if (selectedChannel) {
				this.selectChannel(selectedChannel); // resets current page and updates messages
			}
		}
	};
	
	root.hasActiveFilter = false;
	return root;
})();
