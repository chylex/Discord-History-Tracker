document.addEventListener("DOMContentLoaded", () => {
  DISCORD.setup();
  GUI.setup();
  
  GUI.onFileUploaded(files => {
    if (files.length === 1){
      UTILS.readJsonFile(files[0], (obj, file) => {
        if (SAVEFILE.isValid(obj)){
          STATE.uploadFile(new SAVEFILE(obj));
          updateChannelList();
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
    updateMessageList();
  });
  
  STATE.setMessagesPerPage(GUI.getOptionMessagesPerPage());
  
  GUI.onNavigationButtonClicked(action => {
    STATE.updateCurrentPage(action);
    updateMessageList();
  });
  
  var updateChannelList = function(){
    updateMessageList(null);
    
    GUI.updateChannelList(STATE.getChannelList(), channel => {
      STATE.selectChannel(channel);
      updateMessageList();
    });
  };
  
  var updateMessageList = function(){
    GUI.updateNavigation(STATE.getCurrentPage(), STATE.getPageCount());
    GUI.updateMessageList(STATE.getMessageList());
    GUI.scrollMessagesToTop();
  };
});
