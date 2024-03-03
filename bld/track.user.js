// ==UserScript==
// @name         Discord History Tracker
// @version      v.31h
// @license      MIT
// @namespace    https://chylex.com
// @homepageURL  https://dht.chylex.com/
// @supportURL   https://github.com/chylex/Discord-History-Tracker/issues
// @include      https://discord.com/*
// @run-at       document-idle
// @grant        none
// ==/UserScript==

const start = function(){

// noinspection JSAnnotator

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

// noinspection JSUnresolvedVariable
// noinspection LocalVariableNamingConventionJS
class DISCORD {
	
	// https://discord.com/developers/docs/resources/channel#channel-object-channel-types
	static CHANNEL_TYPE = {
		DM: 1,
		GROUP_DM: 3,
		ANNOUNCEMENT_THREAD: 10,
		PUBLIC_THREAD: 11,
		PRIVATE_THREAD: 12
	};
	
	// https://discord.com/developers/docs/resources/channel#message-object-message-types
	static MESSAGE_TYPE = {
		DEFAULT: 0,
		REPLY: 19,
		THREAD_STARTER: 21
	};
	
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
		const previousMessages = new Set();
		
		const onMessageElementsChanged = function() {
			const messages = DISCORD.getMessages();
			const hasChanged = messages.some(message => !previousMessages.has(message.id)) || !DISCORD.hasMoreMessages();
			
			if (!hasChanged) {
				return;
			}
			
			previousMessages.clear();
			for (const message of messages) {
				previousMessages.add(message.id);
			}
			
			callback(messages);
		};
		
		let debounceTimer;
		
		/**
		 * Do not trigger the callback too often due to autoscrolling.
		 */
		const onMessageElementsChangedLater = function() {
			window.clearTimeout(debounceTimer);
			debounceTimer = window.setTimeout(onMessageElementsChanged, 100);
		};
		
		const observer = new MutationObserver(function () {
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
	}
	
	/**
	 * Returns the message from a message element.
	 * @returns { null | DiscordMessage } }
	 */
	static getMessageFromElement(ele) {
		const props = DOM.getReactProps(ele);
		
		if (props && Array.isArray(props.children)) {
			for (const child of props.children) {
				if (!(child instanceof Object)) {
					continue;
				}
				
				const childProps = child.props;
				if (childProps instanceof Object && "message" in childProps) {
					return childProps.message;
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
					const message = this.getMessageFromElement(ele);
					
					if (message != null) {
						messages.push(message);
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
			let obj = null;
			
			try {
				for (const child of DOM.getReactProps(DOM.queryReactClass("chatContent")).children) {
					if (child && child.props && child.props.channel) {
						obj = child.props.channel;
						break;
					}
				}
			} catch (e) {
				console.error("[DHT] Error retrieving selected channel from 'chatContent' element.", e);
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
					case DISCORD.CHANNEL_TYPE.DM: type = "DM"; break;
					case DISCORD.CHANNEL_TYPE.GROUP_DM: type = "GROUP"; break;
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
				
				if (obj.type === DISCORD.CHANNEL_TYPE.ANNOUNCEMENT_THREAD || obj.type === DISCORD.CHANNEL_TYPE.PUBLIC_THREAD || obj.type === DISCORD.CHANNEL_TYPE.PRIVATE_THREAD) {
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

class DOM {
	/**
	 * Returns a child element by its ID. Parent defaults to the entire document.
	 * @returns {HTMLElement}
	 */
	static id(id, parent) {
		return (parent || document).getElementById(id);
	}
	
	/**
	 * Returns the first child element containing the specified obfuscated class. Parent defaults to the entire document.
	 */
	static queryReactClass(cls, parent) {
		return (parent || document).querySelector(`[class*="${cls}_"]`);
	}
	
	/**
	 * Creates an element, adds it to the DOM, and returns it.
	 */
	static createElement(tag, parent, id, html) {
		/** @type HTMLElement */
		const ele = document.createElement(tag);
		ele.id = id || "";
		ele.innerHTML = html || "";
		parent.appendChild(ele);
		return ele;
	}
	
	/**
	 * Removes an element from the DOM.
	 */
	static removeElement(ele) {
		return ele.parentNode.removeChild(ele);
	}
	
	/**
	 * Creates a new style element with the specified CSS and returns it.
	 */
	static createStyle(styles) {
		return this.createElement("style", document.head, "", styles);
	}
	
	/**
	 * Utility function to save an object into a cookie.
	 */
	static saveToCookie(name, obj, expiresInSeconds) {
		const expires = new Date(Date.now() + 1000 * expiresInSeconds).toUTCString();
		document.cookie = name + "=" + encodeURIComponent(JSON.stringify(obj)) + ";path=/;expires=" + expires;
	}
	
	/**
	 * Utility function to load an object from a cookie.
	 */
	static loadFromCookie(name) {
		const value = document.cookie.replace(new RegExp("(?:(?:^|.*;\\s*)" + name + "\\s*\\=\\s*([^;]*).*$)|^.*$"), "$1");
		return value.length ? JSON.parse(decodeURIComponent(value)) : null;
	}
	
	/**
	 * Returns internal React state object of an element.
	 */
	static getReactProps(ele) {
		const keys = Object.keys(ele || {});
		let key = keys.find(key => key.startsWith("__reactInternalInstance"));
		
		if (key) {
			// noinspection JSUnresolvedVariable
			return ele[key].memoizedProps;
		}
		
		key = keys.find(key => key.startsWith("__reactProps$"));
		return key ? ele[key] : null;
	}
	
	/**
	 * Returns internal React state object of an element, or null if the retrieval throws.
	 */
	static tryGetReactProps(ele) {
		try {
			return this.getReactProps(ele);
		} catch (e) {
			return null;
		}
	}
}

var GUI = (function(){
	var controller;
	var settings;
	
	var updateButtonState = () => {
		if (STATE.isTracking()){
			controller.ui.btnUpload.disabled = true;
			controller.ui.btnSettings.disabled = true;
			controller.ui.btnReset.disabled = true;
		}
		else{
			controller.ui.btnUpload.disabled = false;
			controller.ui.btnSettings.disabled = false;
			controller.ui.btnDownload.disabled = controller.ui.btnReset.disabled = !STATE.hasSavedData();
		}
	};
	
	var stateChangedEvent = (type, detail) => {
		if (controller){
			var force = type === "gui" && detail === "controller";
			
			if (type === "data" || force){
				updateButtonState();
			}
			
			if (type === "tracking" || force){
				updateButtonState();
				controller.ui.btnToggleTracking.innerHTML = STATE.isTracking() ? "Pause Tracking" : "Start Tracking";
			}
			
			if (type === "data" || force){
				var messageCount = 0;
				var channelCount = 0;
				
				if (STATE.hasSavedData()){
					messageCount = STATE.getSavefile().countMessages();
					channelCount = STATE.getSavefile().countChannels();
				}
				
				controller.ui.textStatus.innerHTML = [
					messageCount, " message", (messageCount === 1 ? "" : "s"),
					" from ",
					channelCount, " channel", (channelCount === 1 ? "" : "s")
				].join("");
			}
		}
		
		if (settings){
			var force = type === "gui" && detail === "settings";
			
			if (force){
				settings.ui.cbAutoscroll.checked = SETTINGS.autoscroll;
				settings.ui.optsAfterFirstMsg[SETTINGS.afterFirstMsg].checked = true;
				settings.ui.optsAfterSavedMsg[SETTINGS.afterSavedMsg].checked = true;
			}
			
			if (type === "setting" || force){
				var autoscrollRev = !SETTINGS.autoscroll;
				
				// discord polyfills Object.values
				Object.values(settings.ui.optsAfterFirstMsg).forEach(ele => ele.disabled = autoscrollRev);
				Object.values(settings.ui.optsAfterSavedMsg).forEach(ele => ele.disabled = autoscrollRev);
			}
		}
	};
	
	var registeredEvent = false;
	
	var setupStateChanged = function(detail){
		if (!registeredEvent){
			STATE.onStateChanged(stateChangedEvent);
			SETTINGS.onSettingsChanged((property) =>
				stateChangedEvent("setting", property)
			);
			registeredEvent = true;
		}
		
		stateChangedEvent("gui", detail);
	};
	
	var root = {
		showController: function(){
			controller = {};
			
			// styles
			
			controller.styles = DOM.createStyle(`#app-mount {
  height: calc(100% - 48px) !important;
}

#dht-ctrl {
  position: absolute;
  bottom: 0;
  width: 100%;
  height: 48px;
  background-color: #fff;
  z-index: 1000000;
}

#dht-ctrl button {
  height: 32px;
  margin: 8px 0 8px 8px;
  font-size: 16px;
  padding: 0 12px;
  background-color: #7289da;
  color: #fff;
  text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.75);
}

#dht-ctrl button:disabled {
  background-color: #7a7a7a;
  cursor: default;
}

#dht-ctrl p {
  display: inline-block;
  margin: 14px 12px;
}

#dht-ctrl-close {
  margin: 8px 8px 8px 0 !important;
  float: right;
}
`);
			
			// main
			
			var btn = (id, title) => "<button id='dht-ctrl-"+id+"'>"+title+"</button>";
			
			controller.ele = DOM.createElement("div", document.body, "dht-ctrl", `
${btn("upload", "Upload &amp; Combine")}
${btn("settings", "Settings")}
${btn("track", "")}
${btn("download", "Download")}
${btn("reset", "Reset")}
<p id='dht-ctrl-status'></p>
<input id='dht-ctrl-upload-input' type='file' multiple style="display: none">
${btn("close", "X")}`);
			
			// elements
			
			controller.ui = {
				btnUpload: DOM.id("dht-ctrl-upload"),
				btnSettings: DOM.id("dht-ctrl-settings"),
				btnToggleTracking: DOM.id("dht-ctrl-track"),
				btnDownload: DOM.id("dht-ctrl-download"),
				btnReset: DOM.id("dht-ctrl-reset"),
				btnClose: DOM.id("dht-ctrl-close"),
				textStatus: DOM.id("dht-ctrl-status"),
				inputUpload: DOM.id("dht-ctrl-upload-input")
			};
			
			// events
			
			controller.ui.btnUpload.addEventListener("click", () => {
				controller.ui.inputUpload.click();
			});
			
			controller.ui.btnSettings.addEventListener("click", () => {
				root.showSettings();
			});
			
			controller.ui.btnToggleTracking.addEventListener("click", () => {
				STATE.setIsTracking(!STATE.isTracking());
			});
			
			controller.ui.btnDownload.addEventListener("click", () => {
				STATE.downloadSavefile();
			});
			
			controller.ui.btnReset.addEventListener("click", () => {
				STATE.resetState();
			});
			
			controller.ui.btnClose.addEventListener("click", () => {
				root.hideController();
				window.DHT_ON_UNLOAD.forEach(f => f());
				window.DHT_LOADED = false;
			});
			
			controller.ui.inputUpload.addEventListener("change", () => {
				Array.prototype.forEach.call(controller.ui.inputUpload.files, file => {
					var reader = new FileReader();
					
					reader.onload = function(){
						var obj = {};

						try{
							obj = JSON.parse(reader.result);
						}catch(e){
							alert("Could not parse '"+file.name+"', see console for details.");
							console.error(e);
							return;
						}
						
						if (SAVEFILE.isValid(obj)){
							STATE.uploadSavefile(file.name, new SAVEFILE(obj));
						}
						else{
							alert("File '"+file.name+"' has an invalid format.");
						}
					};
					
					reader.readAsText(file, "UTF-8");
				});

				controller.ui.inputUpload.value = null;
			});
			
			setupStateChanged("controller");
		},
		
		hideController: function(){
			if (controller){
				DOM.removeElement(controller.ele);
				DOM.removeElement(controller.styles);
				controller = null;
			}
		},
		
		showSettings: function(){
			settings = {};
			
			// styles
			
			settings.styles = DOM.createStyle(`#dht-cfg-overlay {
  position: absolute;
  left: 0;
  top: 0;
  width: 100%;
  height: 100%;
  background-color: #000;
  opacity: 0.5;
  display: block;
  z-index: 1000001;
}

#dht-cfg {
  position: absolute;
  left: 50%;
  top: 50%;
  width: 800px;
  height: 262px;
  margin-left: -400px;
  margin-top: -131px;
  padding: 8px;
  background-color: #fff;
  z-index: 1000002;
}

#dht-cfg-note {
  margin-top: 22px;
}

#dht-cfg sub {
  color: #666;
  font-size: 13px;
}
`);
			
			// overlay
			
			settings.overlay = DOM.createElement("div", document.body, "dht-cfg-overlay");
			
			settings.overlay.addEventListener("click", () => {
				root.hideSettings();
			});
			
			// main
			
			var radio = (type, id, label) => "<label><input id='dht-cfg-"+type+"-"+id+"' name='dht-"+type+"' type='radio'> "+label+"</label><br>";
			
			settings.ele = DOM.createElement("div", document.body, "dht-cfg", `
<label><input id='dht-cfg-autoscroll' type='checkbox'> Autoscroll</label><br>
<br>
<label>After reaching the first message in channel...</label><br>
${radio("afm", "nothing", "Continue Tracking")}
${radio("afm", "pause", "Pause Tracking")}
${radio("afm", "switch", "Switch to Next Channel")}
<br>
<label>After reaching a previously saved message...</label><br>
${radio("asm", "nothing", "Continue Tracking")}
${radio("asm", "pause", "Pause Tracking")}
${radio("asm", "switch", "Switch to Next Channel")}
<p id='dht-cfg-note'>
It is recommended to disable link and image previews to avoid putting unnecessary strain on your browser.<br><br>
<sub>v.31h, released 03 March 2024</sub>
</p>`);
			
			// elements
			
			settings.ui = {
				cbAutoscroll: DOM.id("dht-cfg-autoscroll"),
				optsAfterFirstMsg: {},
				optsAfterSavedMsg: {}
			};
			
			settings.ui.optsAfterFirstMsg[CONSTANTS.AUTOSCROLL_ACTION_NOTHING] = DOM.id("dht-cfg-afm-nothing");
			settings.ui.optsAfterFirstMsg[CONSTANTS.AUTOSCROLL_ACTION_PAUSE] = DOM.id("dht-cfg-afm-pause");
			settings.ui.optsAfterFirstMsg[CONSTANTS.AUTOSCROLL_ACTION_SWITCH] = DOM.id("dht-cfg-afm-switch");
			
			settings.ui.optsAfterSavedMsg[CONSTANTS.AUTOSCROLL_ACTION_NOTHING] = DOM.id("dht-cfg-asm-nothing");
			settings.ui.optsAfterSavedMsg[CONSTANTS.AUTOSCROLL_ACTION_PAUSE] = DOM.id("dht-cfg-asm-pause");
			settings.ui.optsAfterSavedMsg[CONSTANTS.AUTOSCROLL_ACTION_SWITCH] = DOM.id("dht-cfg-asm-switch");
			
			// events
			
			settings.ui.cbAutoscroll.addEventListener("change", () => {
				SETTINGS.autoscroll = settings.ui.cbAutoscroll.checked;
			});
			
			Object.keys(settings.ui.optsAfterFirstMsg).forEach(key => {
				settings.ui.optsAfterFirstMsg[key].addEventListener("click", () => {
					SETTINGS.afterFirstMsg = key;
				});
			});
			
			Object.keys(settings.ui.optsAfterSavedMsg).forEach(key => {
				settings.ui.optsAfterSavedMsg[key].addEventListener("click", () => {
					SETTINGS.afterSavedMsg = key;
				});
			});
			
			setupStateChanged("settings");
		},
		
		hideSettings: function(){
			if (settings){
				DOM.removeElement(settings.overlay);
				DOM.removeElement(settings.ele);
				DOM.removeElement(settings.styles);
				settings = null;
			}
		},
		
		setStatus: function(status) {}
	};
	
	return root;
})();

/*
 * SAVEFILE STRUCTURE
 * ==================
 *
 * {
 *   meta: {
 *     users: {
 *       <discord user id>: {
 *         name: <user name>,
 *         avatar: <user icon>,
 *         tag: <user discriminator> // only present if not a bot
 *       }, ...
 *     },
 *
 *     // the user index is an array of discord user ids,
 *     // these indexes are used in the message objects to save space
 *     userindex: [
 *       <discord user id>, ...
 *     ],
 *
 *     servers: [
 *       {
 *         name: <server name>,
 *         type: <"SERVER"|"GROUP"|DM">
 *       }, ...
 *     ],
 *
 *     channels: {
 *       <discord channel id>: {
 *         server: <server index in the meta.servers array>,
 *         name: <channel name>,
 *         position: <order in channel list>, // only present if server type == SERVER
 *         topic: <channel topic>,            // only present if server type == SERVER
 *         nsfw: <channel NSFW status>        // only present if server type == SERVER
 *       }, ...
 *     }
 *   },
 *
 *   data: {
 *     <discord channel id>: {
 *       <discord message id>: {
 *         u: <user index of the sender>,
 *         t: <message timestamp>,
 *         m: <message content>, // only present if not empty
 *         f: <message flags>,   // only present if edited in which case it equals 1, deprecated (use 'te' instead)
 *         te: <edit timestamp>, // only present if edited
 *         e: [ // omit for no embeds
 *           {
 *             url: <embed url>,
 *             type: <embed type>,
 *             t: <rich embed title>,      // only present if type == rich, and if not empty
 *             d: <rich embed description> // only present if type == rich, and if the embed has a simple description text
 *           }, ...
 *         ],
 *         a: [ // omit for no attachments
 *           {
 *             url: <attachment url>
 *           }, ...
 *         ],
 *         r: <reply message id>, // only present if referencing another message (reply)
 *         re: [ // omit for no reactions
 *           {
 *             c: <react count>
 *             n: <emoji name>,
 *             id: <emoji id>,          // only present for custom emoji
 *             an: <emoji is animated>, // only present for custom animated emoji
 *           }, ...
 *         ]
 *       }, ...
 *     }, ...
 *   }
 * }
 *
 *
 * TEMPORARY OBJECT STRUCTURE
 * ==========================
 *
 * {
 *   userlookup: {
 *     <discord user id>: <user index in the meta.userindex array>
 *   },
 *   channelkeys: Set<channel id>,
 *   messagekeys: Set<message id>,
 * }
 */

class SAVEFILE{
	constructor(parsedObj){
		var me = this;
		
		if (!SAVEFILE.isValid(parsedObj)){
			parsedObj = {
				meta: {},
				data: {}
			};
		}
		
		me.meta = parsedObj.meta;
		me.data = parsedObj.data;
		
		me.meta.users = me.meta.users || {};
		me.meta.userindex = me.meta.userindex || [];
		me.meta.servers = me.meta.servers || [];
		me.meta.channels = me.meta.channels || {};
		
		me.tmp = {
			userlookup: {},
			channelkeys: new Set(),
			messagekeys: new Set(),
		};
	}
	
	static isValid(parsedObj){
		return parsedObj && typeof parsedObj.meta === "object" && typeof parsedObj.data === "object";
	}

	static getDate(date){
		if (date instanceof Date) {
			return date;
		}
		else {
			// noinspection JSUnresolvedReference
			return date.toDate();
		}
	};
	
	findOrRegisterUser(userId, userName, userDiscriminator, userAvatar){
		var wasPresent = userId in this.meta.users;
		var userObj = wasPresent ? this.meta.users[userId] : {};
		
		userObj.name = userName;
		
		if (userDiscriminator){
			userObj.tag = userDiscriminator;
		}
		
		if (userAvatar){
			userObj.avatar = userAvatar;
		}
		
		if (!wasPresent){
			this.meta.users[userId] = userObj;
			this.meta.userindex.push(userId);
			return this.tmp.userlookup[userId] = this.meta.userindex.length-1;
		}
		else if (!(userId in this.tmp.userlookup)){
			return this.tmp.userlookup[userId] = this.meta.userindex.findIndex(id => id == userId);
		}
		else{
			return this.tmp.userlookup[userId];
		}
	}
	
	findOrRegisterServer(serverName, serverType){
		var index = this.meta.servers.findIndex(server => server.name === serverName && server.type === serverType);
		
		if (index === -1){
			this.meta.servers.push({
				"name": serverName,
				"type": serverType
			});
			
			return this.meta.servers.length-1;
		}
		else{
			return index;
		}
	}
	
	tryRegisterChannel(serverIndex, channelId, channelName, extraInfo){
		if (!this.meta.servers[serverIndex]){
			return undefined;
		}
		
		var wasPresent = channelId in this.meta.channels;
		var channelObj = wasPresent ? this.meta.channels[channelId] : { "server": serverIndex };
		
		channelObj.name = channelName;
		
		if (extraInfo.position) {
			channelObj.position = extraInfo.position;
		}
		
		if (extraInfo.topic) {
			channelObj.topic = extraInfo.topic;
		}
		
		if (extraInfo.nsfw) {
			channelObj.nsfw = extraInfo.nsfw;
		}
		
		if (wasPresent){
			return false;
		}
		else{
			this.meta.channels[channelId] = channelObj;
			this.tmp.channelkeys.add(channelId);
			return true;
		}
	}
	
	addMessage(channelId, messageId, messageObject){
		var container = this.data[channelId] || (this.data[channelId] = {});
		var wasPresent = messageId in container;
		
		container[messageId] = messageObject;
		this.tmp.messagekeys.add(messageId);
		return !wasPresent;
	}
	
	convertToMessageObject(discordMessage){
		var author = discordMessage.author;
		
		var obj = {
			u: this.findOrRegisterUser(author.id, author.username, author.bot ? null : author.discriminator, author.avatar),
			t: SAVEFILE.getDate(discordMessage.timestamp).getTime()
		};
		
		if (discordMessage.content.length > 0){
			obj.m = discordMessage.content;
		}
		
		if (discordMessage.editedTimestamp !== null){
			obj.te = SAVEFILE.getDate(discordMessage.editedTimestamp).getTime();
		}
		
		if (discordMessage.embeds.length > 0){
			obj.e = discordMessage.embeds.map(embed => {
				let conv = {
					url: embed.url,
					type: embed.type
				};
				
				if (embed.type === "rich"){
					if (Array.isArray(embed.title) && embed.title.length === 1 && typeof embed.title[0] === "string"){
						conv.t = embed.title[0];
						
						if (Array.isArray(embed.description) && embed.description.length === 1 && typeof embed.description[0] === "string"){
							conv.d = embed.description[0];
						}
					}
				}
				
				return conv;
			});
		}
		
		if (discordMessage.attachments.length > 0){
			obj.a = discordMessage.attachments.map(attachment => ({
				url: attachment.url
			}));
		}
		
		if (discordMessage.messageReference !== null){
			obj.r = discordMessage.messageReference.message_id;
		}
		
		if (discordMessage.reactions.length > 0) {
			obj.re = discordMessage.reactions.map(reaction => {
				let conv = {
					c: reaction.count,
					n: reaction.emoji.name
				};
				
				if (reaction.emoji.id !== null) {
					conv.id = reaction.emoji.id;
				}
				
				if (reaction.emoji.animated) {
					conv.an = true;
				}
				
				return conv;
			});
		}
		
		return obj;
	}
	
	addMessagesFromDiscord(discordMessageArray){
		var hasNewMessages = false;
		
		for(var discordMessage of discordMessageArray){
			if (this.addMessage(discordMessage.channel_id, discordMessage.id, this.convertToMessageObject(discordMessage))){
				hasNewMessages = true;
			}
		}
		
		return hasNewMessages;
	}
	
	countChannels(){
		return this.tmp.channelkeys.size;
	}
	
	countMessages(){
		return this.tmp.messagekeys.size;
	}
	
	combineWith(obj){
		var userMap = {};
		var shownError = false;
		
		for(var userId in obj.meta.users){
			var oldUser = obj.meta.users[userId];
			userMap[obj.meta.userindex.findIndex(id => id == userId)] = this.findOrRegisterUser(userId, oldUser.name, oldUser.tag, oldUser.avatar);
		}
		
		for(var channelId in obj.meta.channels){
			var oldServer = obj.meta.servers[obj.meta.channels[channelId].server];
			var oldChannel = obj.meta.channels[channelId];
			this.tryRegisterChannel(this.findOrRegisterServer(oldServer.name, oldServer.type), channelId, oldChannel.name, oldChannel /* filtered later */);
		}
		
		for(var channelId in obj.data){
			var oldChannel = obj.data[channelId];
			
			for(var messageId in oldChannel){
				var oldMessage = oldChannel[messageId];
				var oldUser = oldMessage.u;
				
				if (oldUser in userMap){
					oldMessage.u = userMap[oldUser];
					this.addMessage(channelId, messageId, oldMessage);
				}
				else{
					if (!shownError){
						shownError = true;
						alert("The uploaded archive appears to be corrupted, some messages will be skipped. See console for details.");
						
						console.error("User list:", obj.meta.users);
						console.error("User index:", obj.meta.userindex);
						console.error("Generated mapping:", userMap);
						console.error("Missing user for the following messages:");
					}
					
					console.error(oldMessage);
				}
			}
		}
	}
	
	toJson(){
		return JSON.stringify({
			"meta": this.meta,
			"data": this.data
		});
	}
}

const CONSTANTS = {
	AUTOSCROLL_ACTION_NOTHING: "optNothing",
	AUTOSCROLL_ACTION_PAUSE: "optPause",
	AUTOSCROLL_ACTION_SWITCH: "optSwitch"
};

let IS_FIRST_RUN = false;

const SETTINGS = (function() {
	const settingsChangedEvents = [];
	
	const saveSettings = function() {
		DOM.saveToCookie("DHT_SETTINGS", root, 60 * 60 * 24 * 365 * 5);
	};
	
	const triggerSettingsChanged = function(property) {
		for (const callback of settingsChangedEvents) {
			callback(property);
		}
		
		saveSettings();
	};
	
	const defineTriggeringProperty = function(obj, property, value) {
		const name = "_" + property;
		
		Object.defineProperty(obj, property, {
			get: (() => obj[name]),
			set: (value => {
				obj[name] = value;
				triggerSettingsChanged(property);
			})
		});
		
		obj[name] = value;
	};
	
	let loaded = DOM.loadFromCookie("DHT_SETTINGS");
	
	if (!loaded) {
		loaded = {
			"_autoscroll": true,
			"_afterFirstMsg": CONSTANTS.AUTOSCROLL_ACTION_PAUSE,
			"_afterSavedMsg": CONSTANTS.AUTOSCROLL_ACTION_PAUSE
		};
		
		IS_FIRST_RUN = true;
	}
	
	const root = {
		onSettingsChanged(callback) {
			settingsChangedEvents.push(callback);
		}
	};
	
	defineTriggeringProperty(root, "autoscroll", loaded._autoscroll);
	defineTriggeringProperty(root, "afterFirstMsg", loaded._afterFirstMsg);
	defineTriggeringProperty(root, "afterSavedMsg", loaded._afterSavedMsg);
	
	if (IS_FIRST_RUN) {
		saveSettings();
	}
	
	return root;
})();

// noinspection FunctionWithInconsistentReturnsJS
const STATE = (function() {

	/*
	 * Internal class constructor.
	 */
	class CLS{
		constructor(){
			this._stateChangedEvents = [];
			this._trackingStateChangedListeners = [];
			this.resetState();
		};
		
		_triggerStateChanged(changeType, changeDetail){
			for(var callback of this._stateChangedEvents){
				callback(changeType, changeDetail);
			}
			if (changeType === "tracking") {
				for (let callback of this._trackingStateChangedListeners) {
					callback(this._isTracking);
				}
			}
		};
		
		/*
		 * Resets the state to default values.
		 */
		resetState(){
			this._savefile = null;
			this._isTracking = false;
			this._lastFileName = null;
			this._triggerStateChanged("data", "reset");
		}
		
		/*
		 * Returns the savefile object, creates a new one if needed.
		 */
		getSavefile(){
			if (!this._savefile){
				this._savefile = new SAVEFILE();
			}
			
			return this._savefile;
		}
		
		/*
		 * Returns true if the database file contains any data.
		 */
		hasSavedData(){
			return this._savefile != null;
		}
		
		/*
		 * Returns true if currently tracking message.
		 */
		isTracking(){
			return this._isTracking;
		}
		
		/*
		 * Sets the tracking state.
		 */
		setIsTracking(state){
			this._isTracking = state;
			this._triggerStateChanged("tracking", state);
		}
		
		/*
		 * Combines current savefile with the provided one.
		 */
		uploadSavefile(fileName, fileObject){
			this._lastFileName = fileName;
			this.getSavefile().combineWith(fileObject);
			this._triggerStateChanged("data", "upload");
		}
		
		/*
		 * Triggers a UTF-8 text file download.
		 */
		downloadTextFile(fileName, fileContents) {
			var blob = new Blob([fileContents], { "type": "octet/stream" });
			
			if ("msSaveBlob" in window.navigator){
				return window.navigator.msSaveBlob(blob, fileName);
			}
			
			var url = window.URL.createObjectURL(blob);
			
			var ele = DOM.createElement("a", document.body);
			ele.href = url;
			ele.download = fileName;
			ele.style.display = "none";
			
			ele.click();
			
			document.body.removeChild(ele);
			window.URL.revokeObjectURL(url);
		}
		
		/*
		 * Triggers a savefile download, if available.
		 */
		downloadSavefile(){
			if (this.hasSavedData()){
				this.downloadTextFile(this._lastFileName || "dht.txt", this._savefile.toJson());
			}
		}
		
		/*
		 * Registers a Discord server and channel.
		 */
		addDiscordChannel(serverInfo, channelInfo){
			var serverName = serverInfo.name;
			var serverType = serverInfo.type;
			var channelId = channelInfo.id;
			var channelName = channelInfo.name;
			var extraInfo = channelInfo.extra || {};
			
			var serverIndex = this.getSavefile().findOrRegisterServer(serverName, serverType);
			
			if (this.getSavefile().tryRegisterChannel(serverIndex, channelId, channelName, extraInfo) === true){
				this._triggerStateChanged("data", "channel");
			}
		}
		
		/*
		 * Adds all messages from the array to the specified channel. Returns true if the savefile was updated.
		 */
		addDiscordMessages(discordMessageArray){
			discordMessageArray = discordMessageArray.filter(msg => (msg.type === DISCORD.MESSAGE_TYPE.DEFAULT || msg.type === DISCORD.MESSAGE_TYPE.REPLY || msg.type === DISCORD.MESSAGE_TYPE.THREAD_STARTER) && msg.state === "SENT");
			
			if (this.getSavefile().addMessagesFromDiscord(discordMessageArray)){
				this._triggerStateChanged("data", "messages");
				return true;
			}
			else{
				return false;
			}
		}
		
		/*
		 * Adds a listener that is called whenever the state changes. The callback is a function that takes subject (generic type) and detail (specific type or data).
		 */
		onStateChanged(callback){
			this._stateChangedEvents.push(callback);
		}
		
		/*
		* Shim for code from the desktop app.
		*/
		onTrackingStateChanged(callback) {
			this._trackingStateChangedListeners.push(callback);
			callback(this._isTracking);
		}
	}
	
	return new CLS();
})();


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
			console.debug("[DHT] Reached first message.");
			action = SETTINGS.afterFirstMsg;
		}
		if (isNoAction(action) && !anyNewMessages) {
			console.debug("[DHT] No new messages.");
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
		STATE.addDiscordChannel(info.server, info.channel);
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
			const anyNewMessages = STATE.addDiscordMessages(messages);
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


};

const css = document.createElement("style");

css.innerText = `
#dht-userscript-trigger { cursor: pointer; margin-top: 5px }
#dht-userscript-trigger svg { opacity: 0.6 }
#dht-userscript-trigger:hover svg { opacity: 1 }
`;

document.head.appendChild(css);

window.setInterval(function(){
  if (document.getElementById("dht-userscript-trigger")){
    return;
  }
  
  const help = document.querySelector("section[class^='title'] a[href*='support.discord.com']");
  
  if (help){
    help.insertAdjacentHTML("afterend", `
<span id="dht-userscript-trigger">
  <span style="margin: 0 4px" role="button">
    <svg width="28" height="16" viewBox="0 0 11 6" fill="#fff">
      <path d="M3.133,2.848c0,0.355 -0.044,0.668 -0.132,0.937c-0.088,0.27 -0.208,0.495 -0.36,0.677c-0.153,0.181 -0.333,0.319 -0.541,0.412c-0.207,0.092 -0.431,0.139 -0.672,0.139l-1.413,0l0,-4.266l1.265,0c0.27,0 0.519,0.042 0.746,0.124c0.227,0.083 0.423,0.21 0.586,0.382c0.164,0.171 0.291,0.389 0.383,0.654c0.092,0.264 0.138,0.578 0.138,0.941Zm-0.739,0c0,-0.248 -0.028,-0.461 -0.083,-0.639c-0.056,-0.177 -0.133,-0.323 -0.232,-0.437c-0.099,-0.114 -0.217,-0.198 -0.355,-0.253c-0.139,-0.054 -0.292,-0.082 -0.459,-0.082l-0.518,0l0,2.886l0.621,0c0.147,0 0.283,-0.032 0.409,-0.094c0.125,-0.063 0.233,-0.156 0.325,-0.28c0.092,-0.124 0.163,-0.278 0.215,-0.462c0.051,-0.184 0.077,-0.397 0.077,-0.639Z"></path>
      <path d="M5.939,5.013l0,-1.829l-1.523,0l0,1.829l-0.732,0l0,-4.266l0.732,0l0,1.699l1.523,0l0,-1.699l0.733,0l0,4.266l-0.733,0Z"></path>
      <path d="M8.933,1.437l0,3.576l-0.732,0l0,-3.576l-1.13,0l0,-0.69l2.994,0l0,0.69l-1.132,0Z"></path>
    </svg>
  </span>
</span>`);
    
    document.getElementById("dht-userscript-trigger").addEventListener("click", start);
  }
}, 200);
