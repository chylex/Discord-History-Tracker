if (!window.location.href.includes("discordapp.com/")){
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

let ignoreMessageCallback = false;

let stopTrackingDelayed = function(callback){
  ignoreMessageCallback = true;
  
  DOM.setTimer(() => {
    STATE.toggleTracking();
    ignoreMessageCallback = false;
    
    if (callback){
      callback();
    }
  }, 200); // give the user visual feedback after clicking the button before switching off
};

DISCORD.setupMessageUpdateCallback(hasMoreMessages => {
  if (STATE.isTracking() && !ignoreMessageCallback){
    let info = DISCORD.getSelectedChannel();
    
    if (!info){
      stopTrackingDelayed();
      return;
    }
    
    STATE.addDiscordChannel(info.server, info.type, info.id, info.channel);
    
    let messages = DISCORD.getMessages();
    
    if (!messages.length){
      DISCORD.loadOlderMessages();
      return;
    }
    
    let hasUpdatedFile = STATE.addDiscordMessages(info.id, messages);

    if (SETTINGS.autoscroll){
      let action = CONSTANTS.AUTOSCROLL_ACTION_NOTHING;
      
      if (!hasUpdatedFile && !STATE.isMessageFresh(messages[0].id)){
        action = SETTINGS.afterSavedMsg;
      }
      else if (!hasMoreMessages){
        action = SETTINGS.afterFirstMsg;
      }

      if ((action === CONSTANTS.AUTOSCROLL_ACTION_SWITCH && !DISCORD.selectNextTextChannel()) || action === CONSTANTS.AUTOSCROLL_ACTION_PAUSE){
        STATE.toggleTracking();
      }
      else{
        DISCORD.loadOlderMessages();
      }
    }
  }
});

STATE.onStateChanged((type, enabled) => {
  if (type === "tracking" && enabled){
    let info = DISCORD.getSelectedChannel();
    
    if (info){
      STATE.addDiscordChannel(info.server, info.type, info.id, info.channel);
      STATE.addDiscordMessages(info.id, DISCORD.getMessages());
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
