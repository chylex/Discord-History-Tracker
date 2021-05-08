// noinspection FunctionWithInconsistentReturnsJS
const STATE = (function() {
	/**
	 * @type {{}}
	 * @property {{}} users
	 * @property {String[]} userindex
	 * @property {{}[]} servers
	 * @property {{}} channels
	 */
	let loadedFileMeta;
	let loadedFileData;
	
	let loadedMessages;
	
	let filterFunction;
	let selectedChannel;
	let currentPage;
	let messagesPerPage;
	
	const getUser = function(index) {
		return loadedFileMeta.users[loadedFileMeta.userindex[index]] || { "name": "&lt;unknown&gt;" };
	};
	
	const getUserId = function(index) {
		return loadedFileMeta.userindex[index];
	};
	
	const getUserList = function() {
		return loadedFileMeta ? loadedFileMeta.users : [];
	};
	
	const getChannelList = function() {
		if (!loadedFileMeta) {
			return [];
		}
		
		const channels = loadedFileMeta.channels;
		
		return Object.keys(channels).map(key => ({
			"id": key,
			"name": channels[key].name,
			"server": loadedFileMeta.servers[channels[key].server] || { "name": "&lt;unknown&gt;", "type": "unknown" },
			"msgcount": getFilteredMessageKeys(key).length,
			"topic": channels[key].topic || "",
			"nsfw": channels[key].nsfw || false,
			"position": channels[key].position || -1
		})).sort((ac, bc) => {
			const as = ac.server;
			const bs = bc.server;
			
			return as.type.localeCompare(bs.type, "en") ||
				as.name.toLocaleLowerCase().localeCompare(bs.name.toLocaleLowerCase(), undefined, { numeric: true }) ||
				ac.position - bc.position ||
				ac.name.toLocaleLowerCase().localeCompare(bc.name.toLocaleLowerCase(), undefined, { numeric: true });
		});
	};
	
	const getMessages = function(channel) {
		return loadedFileData[channel] || {};
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
			const avatar = user.avatar ? { id: getUserId(message.u), path: user.avatar } : null;
			
			const reply = ("r" in message && message.r in messages) ? messages[message.r] : null;
			const replyUser = reply ? getUser(reply.u) : null;
			const replyAvatar = replyUser && replyUser.avatar ? { id: getUserId(reply.u), path: replyUser.avatar } : null;
			const replyObj = reply ? {
				"id": message.r,
				"user": replyUser,
				"avatar": replyAvatar,
				"contents": reply.m
			} : null;
			
			return {
				user,
				avatar,
				"timestamp": message.t,
				"contents": ("m" in message) ? message.m : null,
				"embeds": ("e" in message) ? message.e.map(embed => JSON.parse(embed)) : [],
				"attachments": ("a" in message) ? message.a : [],
				"edit": ("te" in message) ? message.te : null,
				"jump": key,
				"reply": replyObj,
				"reactions": ("re" in message) ? message.re : null
			};
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
		
		/**
		 * @param {{ meta, data }} file
		 */
		uploadFile(file) {
			if (loadedFileMeta != null) {
				throw "A file is already loaded!";
			}
			
			if (!file || typeof file.meta !== "object" || typeof file.data !== "object") {
				throw "Invalid file format!";
			}
			
			loadedFileMeta = file.meta;
			loadedFileData = file.data;
			loadedMessages = null;
			
			selectedChannel = null;
			currentPage = 1;
			
			triggerUsersRefreshed();
			triggerChannelsRefreshed();
			triggerMessagesRefreshed();
			
			SETTINGS.onSettingsChanged(() => triggerMessagesRefreshed());
		},
		
		getChannelName(channel) {
			return loadedFileMeta.channels[channel].name || channel;
		},
		
		getUserTag(user) {
			return loadedFileMeta.users[user].tag;
		},
		
		getUserName(user) {
			return loadedFileMeta.users[user].name || user;
		},
		
		selectChannel(channel) {
			currentPage = 1;
			selectedChannel = channel;
			
			loadedMessages = getFilteredMessageKeys(channel).sort(PROCESSOR.SORTER.oldestToNewest);
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
				return 0;
			}
			
			const index = loadedMessages.indexOf(id);
			
			if (index === -1) {
				return 0;
			}
			
			currentPage = Math.max(1, Math.min(this.getPageCount(), 1 + Math.floor(index / messagesPerPage)));
			triggerMessagesRefreshed();
			return index % messagesPerPage;
		},
		
		setActiveFilter(filter) {
			switch (filter ? filter.type : "") {
				case "user":
					filterFunction = PROCESSOR.FILTER.byUser(loadedFileMeta.userindex.indexOf(filter.value));
					break;
				
				case "contents":
					filterFunction = PROCESSOR.FILTER.byContents(filter.value);
					break;
				
				case "withimages":
					filterFunction = PROCESSOR.FILTER.withImages();
					break;
				
				case "withdownloads":
					filterFunction = PROCESSOR.FILTER.withDownloads();
					break;
				
				case "edited":
					filterFunction = PROCESSOR.FILTER.isEdited();
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
