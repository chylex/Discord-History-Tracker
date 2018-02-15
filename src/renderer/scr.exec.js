document.addEventListener("DOMContentLoaded", () => {
  DISCORD.setup();
  GUI.setup();
  
  GUI.onFileUploaded(files => {
    if (files.length === 1){
      UTILS.readJsonFile(files[0], (obj, file) => {
        if (SAVEFILE.isValid(obj)){
          STATE.uploadFile(new SAVEFILE(obj));
        }
        else{
          alert((obj ? "File '{}' has an invalid format." : "Could not parse '{}', see console for details.").replace("{}", file.name));
        }
      });
    }
    else{
      alert("Please, select only one file.");
    }
    
    return true;
  });
  
  GUI.onOptionMessagesPerPageChanged(() => {
    STATE.setMessagesPerPage(GUI.getOptionMessagesPerPage());
  });
  
  STATE.setMessagesPerPage(GUI.getOptionMessagesPerPage());
  
  GUI.onOptionMessagesByUserChanged(() => {
    STATE.setMessagesByUser(GUI.getOptionMessagesByUser());
  });

  GUI.onNavigationButtonClicked(action => {
    STATE.updateCurrentPage(action);
  });
  
  STATE.onChannelsRefreshed(channels => {
    GUI.updateChannelList(channels, STATE.selectChannel);
  });
  
  STATE.onMessagesRefreshed(messages => {
    GUI.updateNavigation(STATE.getCurrentPage(), STATE.getPageCount());
    GUI.updateMessageList(messages);
    GUI.scrollMessagesToTop();
  });

  STATE.onUsersRefreshed(users => {
    GUI.updateUserFilter(users);
  });
});
