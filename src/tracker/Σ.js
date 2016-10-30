if (window.DHT_LOADED){
  alert("Discord History Tracker is already loaded.");
  return;
}

window.DHT_LOADED = true;

if (DISCORD.getSelectedChannel()){
  alert("You ran the script while in a channel, please go to your Friends list, refresh the page, and run the script again to ensure all messages are saved properly.");
}

// Execution

var cachedRequest;
var untrackedRequests = 0;

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
  else{
    ++untrackedRequests;
    
    cachedRequest = {
      channel: channel,
      messages: messages
    };
  }
});

STATE.onStateChanged((type, detail) => {
  if (type === "tracking" && detail){
    var info = DISCORD.getSelectedChannel();
    var isCachedRequestValid = cachedRequest && cachedRequest.channel == info.id;
    
    if (untrackedRequests > 1 || (untrackedRequests === 1 && !isCachedRequestValid)){
      if (!confirm("You have "+untrackedRequests+" untracked request"+(untrackedRequests === 1 ? "" : "s")+", some messages may not be saved until you refresh the page. Do you want to proceed anyway?")){
        STATE.toggleTracking();
        return;
      }
    }
    
    if (isCachedRequestValid){
      STATE.addDiscordChannel(info.server, info.type, cachedRequest.channel, info.channel);
      STATE.addDiscordMessages(cachedRequest.channel, cachedRequest.messages);
      cachedRequest = null;
      --untrackedRequests;
    }
    
    if (STATE.settings.autoscroll && DISCORD.isInMessageView()){
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
  }
});

GUI.showController();
GUI.showSettings();
