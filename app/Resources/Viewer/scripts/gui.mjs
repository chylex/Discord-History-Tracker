import dom from "./dom.mjs";
import discord from "./discord.mjs";
import settings from "./settings.mjs";
import state from "./state.mjs";

export default (function() {
	let eventOnOptMessagesPerPageChanged;
	let eventOnOptMessageFilterChanged;
	let eventOnNavButtonClicked;
	
	const getActiveFilter = function() {
		/** @type HTMLSelectElement */
		const active = dom.fcls("active", dom.id("opt-filter-list"));
		
		return active && active.value !== "" ? {
			"type": active.getAttribute("data-filter-type"),
			"value": active.value
		} : null;
	};
	
	const triggerFilterChanged = function() {
		eventOnOptMessageFilterChanged && eventOnOptMessageFilterChanged(getActiveFilter());
	};
	
	const showModal = function(width, html) {
		const dialog = dom.id("dialog");
		dialog.innerHTML = html;
		dialog.style.width = width + "px";
		dialog.style.marginLeft = (-width / 2) + "px";
		
		dom.id("modal").classList.add("visible");
		return dialog;
	};
	
	// -------------
	// Modal dialogs
	// -------------
	
	const showSettingsModal = function() {
		showModal(560, `
<label><input id='dht-cfg-imgpreviews' type='checkbox'> Image Previews</label><br>
<label><input id='dht-cfg-formatting' type='checkbox'> Message Formatting</label><br>
<label><input id='dht-cfg-useravatars' type='checkbox'> User Avatars</label><br>
<label><input id='dht-cfg-animemoji' type='checkbox'> Animated Emoji</label><br>`);
		
		const setupCheckBox = function(id, settingName) {
			const ele = dom.id(id);
			ele.checked = settings[settingName];
			ele.addEventListener("change", () => settings[settingName] = ele.checked);
		};
		
		setupCheckBox("dht-cfg-imgpreviews", "enableImagePreviews");
		setupCheckBox("dht-cfg-formatting", "enableFormatting");
		setupCheckBox("dht-cfg-useravatars", "enableUserAvatars");
		setupCheckBox("dht-cfg-animemoji", "enableAnimatedEmoji");
	};
	
	const showInfoModal = function() {
		const linkGH = "https://github.com/chylex/Discord-History-Tracker";
		
		showModal(560, `
<p>Discord History Tracker is developed by <a href='https://chylex.com'>chylex</a> as an <a href='${linkGH}/blob/master/LICENSE.md'>open source</a> project.</p>
<p>Please, report any issues and suggestions to the <a href='${linkGH}/issues'>tracker</a>. If you want to support the development, please spread the word and consider <a href='https://www.patreon.com/chylex'>becoming a patron</a> or <a href='https://ko-fi.com/chylex'>buying me a coffee</a>. Any support is appreciated!</p>
<p><a href='${linkGH}/issues'>Issue Tracker</a> &nbsp;&mdash;&nbsp; <a href='${linkGH}'>GitHub Repository</a> &nbsp;&mdash;&nbsp; <a href='https://twitter.com/chylexmc'>Developer's Twitter</a></p>`);
	};
	
	return {
		
		// ---------
		// GUI setup
		// ---------
		
		setup() {
			const inputMessageFilter = dom.id("opt-messages-filter");
			const containerFilterList = dom.id("opt-filter-list");
			
			const resetActiveFilter = function() {
				inputMessageFilter.value = "";
				inputMessageFilter.dispatchEvent(new Event("change"));
				
				dom.id("opt-filter-contents").value = "";
			};
			
			inputMessageFilter.value = ""; // required to prevent browsers from remembering old value
			
			inputMessageFilter.addEventListener("change", () => {
				dom.cls("active", containerFilterList).forEach(ele => ele.classList.remove("active"));
				
				if (inputMessageFilter.value) {
					containerFilterList.querySelector("[data-filter-type='" + inputMessageFilter.value + "']").classList.add("active");
				}
				
				triggerFilterChanged();
			});
			
			Array.prototype.forEach.call(containerFilterList.children, ele => {
				ele.addEventListener(ele.tagName === "SELECT" ? "change" : "input", () => triggerFilterChanged());
			});
			
			dom.id("opt-messages-per-page").addEventListener("change", () => {
				eventOnOptMessagesPerPageChanged && eventOnOptMessagesPerPageChanged();
			});
			
			dom.tag("button", dom.fcls("nav")).forEach(button => {
				button.disabled = true;
				
				button.addEventListener("click", () => {
					eventOnNavButtonClicked && eventOnNavButtonClicked(button.getAttribute("data-nav"));
				});
			});
			
			dom.id("btn-settings").addEventListener("click", () => {
				showSettingsModal();
			});
			
			dom.id("btn-about").addEventListener("click", () => {
				showInfoModal();
			});
			
			dom.id("messages").addEventListener("click", e => {
				const jump = e.target.getAttribute("data-jump");
				
				if (jump) {
					resetActiveFilter();
					
					const index = state.navigateToMessage(jump);
					
					if (index === -1) {
						alert("Message not found.");
					}
					else {
						dom.id("messages").children[index].scrollIntoView();
					}
				}
			});
			
			dom.id("overlay").addEventListener("click", () => {
				dom.id("modal").classList.remove("visible");
				dom.id("dialog").innerHTML = "";
			});
		},
		
		// -----------------
		// Event registering
		// -----------------
		
		/**
		 * Sets a callback for when the user changes the messages per page option. The callback is not passed any arguments.
		 */
		onOptionMessagesPerPageChanged(callback) {
			eventOnOptMessagesPerPageChanged = callback;
		},
		
		/**
		 * Sets a callback for when the user changes the active filter. The callback is passed either null or an object such as { type: <filter type>, value: <filter value> }.
		 */
		onOptMessageFilterChanged(callback) {
			eventOnOptMessageFilterChanged = callback;
		},
		
		/**
		 * Sets a callback for when the user clicks a navigation button. The callback is passed one of the following strings: first, prev, next, last.
		 */
		onNavigationButtonClicked(callback) {
			eventOnNavButtonClicked = callback;
		},
		
		// ----------------------
		// Options and navigation
		// ----------------------
		
		/**
		 * Returns the selected amount of messages per page.
		 */
		getOptionMessagesPerPage() {
			/** @type HTMLInputElement */
			const messagesPerPage = dom.id("opt-messages-per-page");
			return parseInt(messagesPerPage.value, 10);
		},
		
		updateNavigation(currentPage, totalPages) {
			dom.id("nav-page-current").innerHTML = currentPage;
			dom.id("nav-page-total").innerHTML = totalPages || "?";
			
			dom.id("nav-first").disabled = currentPage === 1;
			dom.id("nav-prev").disabled = currentPage === 1;
			dom.id("nav-pick").disabled = (totalPages || 0) <= 1;
			dom.id("nav-next").disabled = currentPage === (totalPages || 1);
			dom.id("nav-last").disabled = currentPage === (totalPages || 1);
		},
		
		// --------------
		// Updating lists
		// --------------
		
		/**
		 * Updates the channel list and sets up their click events. The callback is triggered whenever a channel is selected, and takes the channel ID as its argument.
		 */
		updateChannelList(channels, selected, callback) {
			const eleChannels = dom.id("channels");
			
			if (!channels) {
				eleChannels.innerHTML = "";
			}
			else {
				if (getActiveFilter() != null) {
					channels = channels.filter(channel => channel.msgcount > 0);
				}
				
				eleChannels.innerHTML = channels.map(channel => discord.getChannelHTML(channel)).join("");
				
				Array.prototype.forEach.call(eleChannels.children, ele => {
					ele.addEventListener("click", () => {
						const currentChannel = dom.fcls("active", eleChannels);
						
						if (currentChannel) {
							currentChannel.classList.remove("active");
						}
						
						ele.classList.add("active");
						callback(ele.getAttribute("data-channel"));
					});
				});
				
				if (selected) {
					const activeChannel = eleChannels.querySelector("[data-channel='" + selected + "']");
					activeChannel && activeChannel.classList.add("active");
				}
			}
		},
		
		updateMessageList(messages) {
			dom.id("messages").innerHTML = messages ? messages.map(message => discord.getMessageHTML(message)).join("") : "";
		},
		
		updateUserList(users) {
			/** @type HTMLSelectElement */
			const eleSelect = dom.id("opt-filter-user");
			
			while (eleSelect.length > 1) {
				eleSelect.remove(1);
			}
			
			const options = [];
			
			for (const id of Object.keys(users)) {
				const user = users[id];
				const option = document.createElement("option");
				option.value = id;
				option.text = user.displayName ? `${user.displayName} (${user.name})` : user.name;
				options.push(option);
			}
			
			options.sort((a, b) => a.text.toLocaleLowerCase().localeCompare(b.text.toLocaleLowerCase()));
			options.forEach(option => eleSelect.add(option));
		},
		
		scrollMessagesToTop() {
			dom.id("messages").scrollTop = 0;
		}
	};
})();
