var STATE = (function(){
  var stateChangedEvents = [];
  
  var triggerStateChanged = function(changeType, changeDetail){
    for(var callback of stateChangedEvents){
      callback(changeType, changeDetail);
    }
  };
  
  /*
   * Internal class constructor.
   */
  var CLS = function(){
    this.resetState();
  };
  
  /*
   * Resets the state to default values.
   */
  CLS.prototype.resetState = function(){
    this._savefile = null;
    this._isTracking = false;
    this._lastFileName = null;
    triggerStateChanged("data", "reset");
  };
  
  /*
   * Returns the savefile object, creates a new one if needed.
   */
  CLS.prototype.getSavefile = function(){
    if (!this._savefile){
      this._savefile = new SAVEFILE();
    }
    
    return this._savefile;
  };
  
  /*
   * Returns true if the database file contains any data.
   */
  CLS.prototype.hasSavedData = function(){
    return this._savefile != null;
  };
  
  /*
   * Returns true if currently tracking message.
   */
  CLS.prototype.isTracking = function(){
    return this._isTracking;
  };
  
  /*
   * Toggles the tracking state.
   */
  CLS.prototype.toggleTracking = function(){
    this._isTracking = !this._isTracking;
    triggerStateChanged("tracking", this._isTracking);
  };
  
  /*
   * Toggles the tracking state.
   */
  CLS.prototype.toggleTracking = function(){
    this._isTracking = !this._isTracking;
    triggerStateChanged("tracking", this._isTracking);
  };
  
  /*
   * Combines current savefile with the provided one.
   */
  CLS.prototype.uploadSavefile = function(fileName, fileObject){
    this._lastFileName = fileName;
    this.getSavefile().combineWith(fileObject);
    triggerStateChanged("data", "upload");
  };
  
  /*
   * Triggers a savefile download, if available.
   */
  CLS.prototype.downloadSavefile = function(){
    if (this.hasSavedData()){
      DOM.downloadTextFile(this._lastFileName || "dht.txt", this._savefile.toJson());
    }
  };
  
  /*
   * Registers a Discord server and channel.
   */
  CLS.prototype.addDiscordChannel = function(serverName, serverType, channelId, channelName){
    var serverIndex = this.getSavefile().findOrRegisterServer(serverName, serverType);
    
    if (this.getSavefile().tryRegisterChannel(serverIndex, channelId, channelName) === true){
      triggerStateChanged("data", "channel");
    }
  };
  
  /*
   * Adds all messages from the array to the specified channel. Returns true if the savefile was updated.
   */
  CLS.prototype.addDiscordMessages = function(channelId, discordMessageArray){
    if (this.getSavefile().addMessagesFromDiscord(channelId, discordMessageArray)){
      triggerStateChanged("data", "messages");
      return true;
    }
    else{
      return false;
    }
  };
  
  /*
   * Returns true if the message was added during this session.
   */
  CLS.prototype.isMessageFresh = function(id){
    return this.getSavefile().isMessageFresh(id);
  };
  
  /*
   * Adds a listener that is called whenever the state changes. The callback is a function that takes subject (generic type) and detail (specific type or data).
   */
  CLS.prototype.onStateChanged = function(callback){
    stateChangedEvents.push(callback);
  };
  
  return new CLS();
})();
