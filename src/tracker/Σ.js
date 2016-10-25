DISCORD.setupMessageRequestHook((channel, messages) => {
  if (STATE.isTracking()){
    var info = DISCORD.getSelectedChannel();
    
    if (info.id == channel){ // Discord has a bug where the message request may be sent without switching channels
      STATE.addDiscordChannel(info.server, info.type, channel, info.channel);
      STATE.addDiscordMessages(channel, messages);
    }
  }
});

GUI.showController();
GUI.showSettings();
