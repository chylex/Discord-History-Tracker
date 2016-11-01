var STATE = (function(){
  var FILE;
  var MSGS;
  
  var selectedChannel;
  var currentPage;
  var messagesPerPage;
  
  var getPageCount = function(){
    return !MSGS ? 0 : (!messagesPerPage ? 1 : Math.ceil(MSGS.length/messagesPerPage));
  };
  
  return {
    uploadFile: function(file){
      FILE = file;
      MSGS = null;
      currentPage = 1;
    },
    
    getChannelList: function(){
      var channels = FILE.getChannels();
      
      return Object.keys(channels).map(key => ({ // reserve.txt
        id: key,
        name: channels[key].name,
        server: FILE.getServer(channels[key].server),
        msgcount: FILE.getMessageCount(key)
      }));
    },
    
    getChannelName: function(channel){
      return FILE.getChannelById(channel).name;
    },
    
    getUserName: function(user){
      return FILE.getUserById(user).name;
    },
    
    selectChannel: function(channel){
      selectedChannel = channel;
      currentPage = 1;
      
      MSGS = Object.keys(FILE.getMessages(channel)).sort((key1, key2) => {
        if (key1.length === key2.length){
          return key1 > key2 ? 1 : key1 < key2 ? -1 : 0;
        }
        else{
          return key1.length > key2.length ? 1 : -1;
        }
      });
    },
    
    getSelectedChannel: function(){
      return selectedChannel;
    },
    
    getRawMessages: function(channel){
      return channel ? FILE.getMessages(channel) : FILE.getAllMessages();
    },
    
    setMessagesPerPage: function(amount){
      messagesPerPage = amount;
    },
    
    updateCurrentPage: function(action){
      switch(action){
        case "first": currentPage = 1; break;
        case "prev": currentPage = Math.max(1, currentPage-1); break;
        case "next": currentPage = Math.min(STATE.getPageCount(), currentPage+1); break;
        case "last": currentPage = STATE.getPageCount(); break;
      }
    },
    
    getMessageList: function(){
      if (!MSGS){
        return [];
      }
      
      var messages = FILE.getMessages(selectedChannel);
      var startIndex = messagesPerPage*(currentPage-1);
      
      return MSGS.slice(startIndex, !messagesPerPage ? undefined : startIndex+messagesPerPage).map(key => {
        var message = messages[key];
        
        return { // reserve.txt
          user: FILE.getUser(message.u),
          timestamp: message.t,
          contents: message.m,
          embeds: message.e,
          attachments: message.a,
          edited: (message.f&1) === 1
        };
      });
    },
    
    getCurrentPage: function(){
      var total = getPageCount();
      
      if (currentPage > total && total > 0){
        currentPage = total;
      }
      
      return currentPage;
    },
    
    getPageCount: function(){
      return getPageCount();
    }
  };
})();
