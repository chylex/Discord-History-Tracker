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
  class CLS{
    constructor(){
      this.resetState();
    };
    
    /*
     * Resets the state to default values.
     */
    resetState(){
      this._savefile = null;
      this._isTracking = false;
      this._lastFileName = null;
      triggerStateChanged("data", "reset");
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
      triggerStateChanged("tracking", state);
    }
    
    /*
     * Combines current savefile with the provided one.
     */
    uploadSavefile(fileName, fileObject){
      this._lastFileName = fileName;
      this.getSavefile().combineWith(fileObject);
      triggerStateChanged("data", "upload");
    }
    
    /*
     * Triggers a savefile download, if available.
     */
    downloadSavefile(){
      if (this.hasSavedData()){
        DOM.downloadTextFile(this._lastFileName || "dht.txt", this._savefile.toJson());
      }
    }
    
    /*
     * Registers a Discord server and channel.
     */
    addDiscordChannel(serverName, serverType, channelId, channelName, extraInfo){
      var serverIndex = this.getSavefile().findOrRegisterServer(serverName, serverType);
      
      if (this.getSavefile().tryRegisterChannel(serverIndex, channelId, channelName, extraInfo) === true){
        triggerStateChanged("data", "channel");
      }
    }
    
    /*
     * Adds all messages from the array to the specified channel. Returns true if the savefile was updated.
     */
    addDiscordMessages(channelId, discordMessageArray){
      if (this.getSavefile().addMessagesFromDiscord(channelId, discordMessageArray)){
        triggerStateChanged("data", "messages");
        return true;
      }
      else{
        return false;
      }
    }
    
    /*
     * Returns true if the message was added during this session.
     */
    isMessageFresh(id){
      return this.getSavefile().isMessageFresh(id);
    }
    
    /*
     * Adds a listener that is called whenever the state changes. The callback is a function that takes subject (generic type) and detail (specific type or data).
     */
    onStateChanged(callback){
      stateChangedEvents.push(callback);
    }
  }
  
  return new CLS();
})();
