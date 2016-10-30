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
        updateNavigation(true);
      };
      
      reader.readAsText(files[0], "UTF-8");
    }
    else{
      alert("Please, select only one file.");
    }
    
    return true;
  });
  
  GUI.onOptionMessagesPerPageChanged(amount => {
    updateMessageList();
  });
  
  GUI.onNavigationButtonClicked(action => {
    switch(action){
      case "first": currentPage = 1; break;
      case "prev": currentPage = Math.max(1, currentPage-1); break;
      case "next": currentPage = Math.min(getTotalPageCount(), currentPage+1); break;
      case "last": currentPage = getTotalPageCount(); break;
    }
    
    updateMessageList();
  });
  
  var currentPage = 1;
  
  var updateChannelList = function(){
    GUI.updateChannelList(STATE.getChannelList(), channel => {
      STATE.selectChannel(channel);
      currentPage = 1;
      updateMessageList();
    });
    
    updateMessageList(null);
  };
  
  var updateMessageList = function(){
    var mpp = GUI.getOptionMessagesPerPage();
    
    GUI.updateMessageList(STATE.getMessageList(mpp*(currentPage-1), mpp));
    GUI.scrollMessagesToTop();
    updateNavigation(false);
  };
  
  var updateNavigation = function(reset){
    var total = getTotalPageCount();
    
    if (reset){
      currentPage = 1;
    }
    else if (currentPage > total && total > 0){
      currentPage = total;
    }
    
    GUI.updateNavigation(currentPage, total);
  };
  
  var getTotalPageCount = function(){
    return Math.ceil(STATE.getMessageCount()/GUI.getOptionMessagesPerPage());
  };
});
