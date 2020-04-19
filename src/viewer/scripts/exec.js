document.addEventListener("DOMContentLoaded", () => {
  var embedded = EMBED.getEmbeddedJSON();
  
  if (location.search === "?embed" && !embedded){
    EMBED.setup();
  }
  
  DISCORD.setup();
  GUI.setup();
  
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
  
  var loadJSON = function(json, errParse, errInvalid){
    var obj;
    
    try{
      obj = JSON.parse(json);
      EMBED.onFileRead(json);
    }catch(e){
      console.error(e);
      alert(errParse);
      return;
    }
    
    if (SAVEFILE.isValid(obj)){
      STATE.uploadFile(new SAVEFILE(obj));
    }
    else{
      alert(errInvalid);
    }
  };
  
  if (embedded){
    loadJSON(embedded, "Could not parse embedded file, see console for details.", "Embedded file has an invalid format.");
  }
  else{
    GUI.onFileUploaded(files => {
      if (files.length === 1){
        var file = files[0];
        var reader = new FileReader();
        
        STATE.setUploadedFileName(file.name);

        reader.onload = () => loadJSON(reader.result, "Could not parse '"+file.name+"', see console for details.", "File '"+file.name+"' has an invalid format.");
        reader.readAsText(file, "UTF-8");
      }
      else{
        alert("Please, select only one file.");
      }
      
      return true;
    });
  }
});
