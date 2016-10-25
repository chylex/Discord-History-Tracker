var STATE = (function(){
  var stateChangedEvents = [];
  
  var triggerStateChanged = function(changeType, changeDetail){
    for(var callback of stateChangedEvents){
      callback(changeType, changeDetail);
    }
  };
  
  var defineTriggeringProperty = function(obj, type, property){
    var name = "_"+property;
    
    Object.defineProperty(obj, property, {
      get: (() => obj[name]),
      set: (value => {
        obj[name] = value;
        triggerStateChanged(type, property);
      })
    });
  };
  
  /*
   * Internal settings class constructor.
   */
  var SETTINGS = function(){
  };
  
  /*
   * Resets settings without triggering state changed event.
   */
  SETTINGS.prototype._reset = function(){
    this._autoscroll = true;
  };
  
  /*
   * Internal class constructor.
   */
  var CLS = function(){
    this.settings = new SETTINGS();
    this.resetState();
  };
  
  /*
   * Resets the state to default values.
   */
  CLS.prototype.resetState = function(){
    this._savefile = null;
    this._isTracking = false;
    this.settings._reset();
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
   * Combines current savefile with the provided one.
   */
  CLS.prototype.uploadSavefile = function(readFile){
    this.getSavefile().combineWith(readFile);
    triggerStateChanged("data", "upload");
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
      triggerStateChanged("data", "channel");
    }
  };
  
  /*
   * Adds all messages from the array to the specified channel.
   */
  CLS.prototype.addDiscordMessages = function(channelId, discordMessageArray){
    if (this.getSavefile().addMessagesFromDiscord(channelId, discordMessageArray)){
      triggerStateChanged("data", "messages");
    }
  };
  
  /*
   * Adds a listener that is called whenever the state changes. The callback is a function that takes subject (generic type) and detail (specific type or data).
   */
  CLS.prototype.onStateChanged = function(callback){
    stateChangedEvents.push(callback);
  };
  
  return new CLS();
})();
