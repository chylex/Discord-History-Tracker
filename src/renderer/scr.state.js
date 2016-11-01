var STATE = (function(){
  var FILE;
  var MSGS;
  
  var selectedChannel;
  
  return {
    uploadFile: function(file){
      FILE = file;
      MSGS = null;
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
    
    getMessageList: function(startIndex, count){
      if (!MSGS){
        return [];
      }
      
      var messages = FILE.getMessages(selectedChannel);
      
      return MSGS.slice(startIndex, !count ? undefined : startIndex+count).map(key => {
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
    
    getMessageCount: function(){
      return MSGS ? MSGS.length : 0;
    }
  };
})();
