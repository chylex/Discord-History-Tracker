document.addEventListener("DOMContentLoaded", () => {
  DISCORD.setup();
  GUI.setup();
  
  GUI.onFileUploaded(files => {
    if (files.length === 1){
      var reader = new FileReader();
      
      reader.onload = function(){
        var obj = {};
        
        try{
          obj = JSON.parse(reader.result);
        }catch(e){
          alert("Could not parse '"+file.name+"', see console for details.");
          console.error(e);
          return;
        }
        
        if (!SAVEFILE.isValid(obj)){
          alert("File '"+file.name+"' has an invalid format.");
          return;
        }
        
        STATE.uploadFile(new SAVEFILE(obj));
        updateChannelList();
      };
      
      reader.readAsText(files[0], "UTF-8");
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
    GUI.updateChannelList(STATE.getChannelList(), channel => {
      STATE.selectChannel(channel);
      updateMessageList();
    });
    
    updateMessageList(null);
  };
  
  var updateMessageList = function(){
    GUI.updateNavigation(STATE.getCurrentPage(), STATE.getPageCount());
    GUI.updateMessageList(STATE.getMessageList());
    GUI.scrollMessagesToTop();
  };
});
