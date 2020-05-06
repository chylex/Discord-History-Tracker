if (!window.location.href.includes("discord.com/")){
  if (!confirm("Could not detect Discord in the URL, do you want to run the script anyway?")){
    return;
  }
}

if (window.DHT_LOADED){
  alert("Discord History Tracker is already loaded.");
  return;
}

window.DHT_LOADED = true;
window.DHT_ON_UNLOAD = [];

// Execution

let ignoreMessageCallback = new Set();

let stopTrackingDelayed = function(callback){
  ignoreMessageCallback.add("stopping");
  
  DOM.setTimer(() => {
    STATE.setIsTracking(false);
    ignoreMessageCallback.delete("stopping");
    
    if (callback){
      callback();
    }
  }, 200); // give the user visual feedback after clicking the button before switching off
};

DISCORD.setupMessageUpdateCallback(hasMoreMessages => {
  if (STATE.isTracking() && ignoreMessageCallback.size === 0){
    let info = DISCORD.getSelectedChannel();
    
    if (!info){
      stopTrackingDelayed();
      return;
    }
    
    STATE.addDiscordChannel(info.server, info.type, info.id, info.channel);
    
    let messages = DISCORD.getMessages();
    
    if (messages == null){
      stopTrackingDelayed();
      return;
    }
    else if (!messages.length){
      DISCORD.loadOlderMessages();
      return;
    }
    
    let hasUpdatedFile = STATE.addDiscordMessages(info.id, messages);
    
    if (SETTINGS.autoscroll){
      let action = null;
      
      if (!hasUpdatedFile && !STATE.isMessageFresh(messages[0].id)){
        action = SETTINGS.afterSavedMsg;
      }
      else if (!hasMoreMessages){
        action = SETTINGS.afterFirstMsg;
      }
      
      if (action === null){
        DISCORD.loadOlderMessages();
      }
      else{
        ignoreMessageCallback.add("stalling");
        
        DOM.setTimer(() => {
          ignoreMessageCallback.delete("stalling");
          
          let updatedInfo = DISCORD.getSelectedChannel();
          
          if (updatedInfo && updatedInfo.id === info.id){
            let lastMessages = DISCORD.getMessages(); // sometimes needed to catch the last few messages before switching
            
            if (lastMessages != null){
              STATE.addDiscordMessages(info.id, lastMessages);
            }
          }
          
          if ((action === CONSTANTS.AUTOSCROLL_ACTION_SWITCH && !DISCORD.selectNextTextChannel()) || action === CONSTANTS.AUTOSCROLL_ACTION_PAUSE){
            STATE.setIsTracking(false);
          }
        }, 250);
      }
    }
  }
});

STATE.onStateChanged((type, enabled) => {
  if (type === "tracking" && enabled){
    let info = DISCORD.getSelectedChannel();
    
    if (info){
      let messages = DISCORD.getMessages();
      
      if (messages != null){
        STATE.addDiscordChannel(info.server, info.type, info.id, info.channel);
        STATE.addDiscordMessages(info.id, messages);
      }
      else{
        stopTrackingDelayed(() => alert("Cannot see any messages."));
        return;
      }
    }
    else{
      stopTrackingDelayed(() => alert("The selected channel is not visible in the channel list."));
      return;
    }
    
    if (SETTINGS.autoscroll && DISCORD.isInMessageView()){
      if (DISCORD.hasMoreMessages()){
        DISCORD.loadOlderMessages();
      }
      else{
        let action = SETTINGS.afterFirstMsg;
        
        if ((action === CONSTANTS.AUTOSCROLL_ACTION_SWITCH && !DISCORD.selectNextTextChannel()) || action === CONSTANTS.AUTOSCROLL_ACTION_PAUSE){
          stopTrackingDelayed();
        }
      }
    }
  }
});

GUI.showController();

if (IS_FIRST_RUN){
  GUI.showSettings();
}
