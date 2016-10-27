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
      
      return Object.keys(channels).map(key => ({
        id: key,
        name: channels[key].name,
        server: FILE.getServer(channels[key].server),
        msgcount: FILE.getMessageCount(key)
      }));
    },
    
    selectChannel: function(channel){
      selectedChannel = channel;
      MSGS = Object.keys(FILE.getMessages(channel)).sort();
    },
    
    getMessageList: function(startIndex, count){
      if (!MSGS){
        return [];
      }
      
      var messages = FILE.getMessages(selectedChannel);
      
      return MSGS.slice(startIndex, !count ? undefined : startIndex+count).map(key => {
        var message = messages[key];
        
        return {
          user: FILE.getUser(message.u),
          timestamp: message.t,
          contents: message.m
        };
      });
    }
  };
})();
