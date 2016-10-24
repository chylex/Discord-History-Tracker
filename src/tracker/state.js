var STATE = (function(){
  var stateChangedEvents = [];
  
  var triggerStateChanged = function(){
    for(var callback of stateChangedEvents){
      callback();
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
    triggerStateChanged();
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
    triggerStateChanged();
  };
  
  /*
   * Combines current savefile with the provided one.
   */
  CLS.prototype.uploadSavefile = function(readFile){
    this.getSavefile().combineWith(readFile);
    triggerStateChanged();
  };
  
  /*
   * Triggers a savefile download, if available.
   */
  CLS.prototype.downloadSavefile = function(){
    if (this.hasSavedData()){
      DOM.downloadTextFile("dht.txt", this._savefile.toJson());
    }
  };
  
  /*
   * Registers a Discord server and channel.
   */
  CLS.prototype.addDiscordChannel = function(serverName, serverType, channelId, channelName){
    var serverIndex = this.getSavefile().findOrRegisterServer(serverName, serverType);
    
    if (this.getSavefile().tryRegisterChannel(serverIndex, channelId, channelName) === true){
      triggerStateChanged();
    }
  };
  
  /*
   * Adds all messages from the array to the specified channel.
   */
  CLS.prototype.addDiscordMessages = function(channelId, discordMessageArray){
    if (this.getSavefile().addMessagesFromDiscord(channelId, discordMessageArray)){
      triggerStateChanged();
    }
  };
  
  /*
   * Adds a listener that is called whenever the state changes. If trigger is true, the callback is ran after adding it to the listener list.
   */
  CLS.prototype.onStateChanged = function(callback, trigger){
    stateChangedEvents.push(callback);
    trigger && callback();
  };
  
  return new CLS();
})();
