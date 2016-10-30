if (window.DHT_LOADED){
  alert("Discord History Tracker is already loaded.");
  return;
}

window.DHT_LOADED = true;

DISCORD.setupMessageRequestHook((channel, messages) => {
  if (STATE.isTracking()){
    var info = DISCORD.getSelectedChannel();
    
    if (info.id == channel){ // Discord has a bug where the message request may be sent without switching channels
      STATE.addDiscordChannel(info.server, info.type, channel, info.channel);
      var hasUpdatedFile = STATE.addDiscordMessages(channel, messages);
      
      if (STATE.settings.autoscroll){
        DOM.setTimer(() => {
          var action = CONSTANTS.AUTOSCROLL_ACTION_NOTHING;
          
          if (!hasUpdatedFile){
            action = STATE.settings.afterSavedMsg;
          }
          else if (!DISCORD.hasMoreMessages()){
            action = STATE.settings.afterFirstMsg;
          }
          
          if ((action === CONSTANTS.AUTOSCROLL_ACTION_SWITCH && !DISCORD.selectNextTextChannel()) || action === CONSTANTS.AUTOSCROLL_ACTION_PAUSE){
            STATE.toggleTracking();
          }
          else{
            DISCORD.loadOlderMessages();
          }
        }, 0);
      }
    }
  }
});

STATE.onStateChanged((type, detail) => {
  if (type === "tracking" && detail && STATE.settings.autoscroll && DISCORD.isInMessageView()){
    if (DISCORD.hasMoreMessages()){
      DISCORD.loadOlderMessages();
    }
    else{
      var action = STATE.settings.afterFirstMsg;
      
      if ((action === CONSTANTS.AUTOSCROLL_ACTION_SWITCH && !DISCORD.selectNextTextChannel()) || action === CONSTANTS.AUTOSCROLL_ACTION_PAUSE){
        DOM.setTimer(() => STATE.toggleTracking(), 200); // give the user visual feedback after clicking the button before switching off
      }
    }
  }
});

GUI.showController();
GUI.showSettings();
