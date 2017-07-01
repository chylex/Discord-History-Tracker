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

DISCORD.setupMessageUpdateCallback(channel => {
  if (STATE.isTracking()){
    var info = DISCORD.getSelectedChannel();
    STATE.addDiscordChannel(info.server, info.type, info.id, info.channel);
    
    var hasUpdatedFile = STATE.addDiscordMessages(info.id, DISCORD.getMessages());

    if (SETTINGS.autoscroll){
      DOM.setTimer(() => {
        var action = CONSTANTS.AUTOSCROLL_ACTION_NOTHING;

        if (!hasUpdatedFile){
          action = SETTINGS.afterSavedMsg;
        }
        else if (!DISCORD.hasMoreMessages()){
          action = SETTINGS.afterFirstMsg;
        }

        if ((action === CONSTANTS.AUTOSCROLL_ACTION_SWITCH && !DISCORD.selectNextTextChannel()) || action === CONSTANTS.AUTOSCROLL_ACTION_PAUSE){
          STATE.toggleTracking();
        }
        else{
          DISCORD.loadOlderMessages();
        }
      }, 50);
    }
  }
});

STATE.onStateChanged((type, enabled) => {
  if (type === "tracking" && enabled){
    var info = DISCORD.getSelectedChannel();
    
    if (info){
      STATE.addDiscordChannel(info.server, info.type, info.id, info.channel);
      STATE.addDiscordMessages(info.id, DISCORD.getMessages());
    }
    
    if (SETTINGS.autoscroll && DISCORD.isInMessageView()){
      if (DISCORD.hasMoreMessages()){
        DISCORD.loadOlderMessages();
      }
      else{
        var action = SETTINGS.afterFirstMsg;

        if ((action === CONSTANTS.AUTOSCROLL_ACTION_SWITCH && !DISCORD.selectNextTextChannel()) || action === CONSTANTS.AUTOSCROLL_ACTION_PAUSE){
          DOM.setTimer(() => STATE.toggleTracking(), 200); // give the user visual feedback after clicking the button before switching off
        }
      }
    }
  }
});

GUI.showController();

if (IS_FIRST_RUN){
  GUI.showSettings();
}
