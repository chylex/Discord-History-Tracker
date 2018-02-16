document.addEventListener("DOMContentLoaded", () => {
  DISCORD.setup();
  GUI.setup();
  
  GUI.onFileUploaded(files => {
    if (files.length === 1){
      var file = files[0];
      var reader = new FileReader();

      reader.onload = function(){
        var obj;
        
        try{
          obj = JSON.parse(reader.result);
        }catch(e){
          console.error(e);
          alert("Could not parse '"+file.name+"', see console for details.");
          return;
        }
        
        if (SAVEFILE.isValid(obj)){
          STATE.uploadFile(new SAVEFILE(obj));
        }
        else{
          alert("File '"+file.name+"' has an invalid format.");
        }
      };

      reader.readAsText(file, "UTF-8");
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
  
  GUI.onOptMessageFilterChanged(filter => {
    STATE.setActiveFilter(filter);
  });
  
  GUI.onNavigationButtonClicked(action => {
    STATE.updateCurrentPage(action);
  });
  
  STATE.onUsersRefreshed(users => {
    GUI.updateUserList(users);
  });
  
  STATE.onChannelsRefreshed((channels, selected) => {
    GUI.updateChannelList(channels, selected, STATE.selectChannel);
  });
  
  STATE.onMessagesRefreshed(messages => {
    GUI.updateNavigation(STATE.getCurrentPage(), STATE.getPageCount());
    GUI.updateMessageList(messages);
    GUI.scrollMessagesToTop();
  });
});
