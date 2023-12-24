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
