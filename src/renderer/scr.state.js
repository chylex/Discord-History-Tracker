var STATE = (function(){
  var ROOT = {};
  
  // ---------------
  // State variables
  // ---------------
  
  var FILE;
  var MSGS;
  
  var selectedChannel;
  var currentPage;
  var messagesPerPage;
  
  // ----------------------------------
  // Channel and message refresh events
  // ----------------------------------
  
  var eventOnChannelsRefreshed;
  var eventOnMessagesRefreshed;
  
  var triggerChannelsRefreshed = function(){
    eventOnChannelsRefreshed && eventOnChannelsRefreshed(ROOT.getChannelList());
  };
  
  var triggerMessagesRefreshed = function(){
    eventOnMessagesRefreshed && eventOnMessagesRefreshed(ROOT.getMessageList());
  };
  
  ROOT.onChannelsRefreshed = function(callback){
    eventOnChannelsRefreshed = callback;
  };
    
  ROOT.onMessagesRefreshed = function(callback){
    eventOnMessagesRefreshed = callback;
  };
  
  // ------------------------------------
  // File upload and basic data retrieval
  // ------------------------------------

  ROOT.uploadFile = function(file){
    FILE = file;
    MSGS = null;
    currentPage = 1;

    triggerChannelsRefreshed();
    triggerMessagesRefreshed();
  };

  ROOT.getChannelName = function(channel){
    return FILE.getChannelById(channel).name;
  };

  ROOT.getUserName = function(user){
    return FILE.getUserById(user).name;
  };

  ROOT.getRawMessages = function(channel){
    return channel ? FILE.getMessages(channel) : FILE.getAllMessages();
  };
  
  // --------------------------
  // Channel list and selection
  // --------------------------

  ROOT.getChannelList = function(){
    var channels = FILE.getChannels();

    return Object.keys(channels).map(key => ({ // reserve.txt
      id: key,
      name: channels[key].name,
      server: FILE.getServer(channels[key].server),
      msgcount: FILE.getMessageCount(key)
    }));
  };

  ROOT.selectChannel = function(channel){
    currentPage = 1;
    selectedChannel = channel;
    MSGS = Object.keys(FILE.getMessages(channel)).sort(PROCESSOR.SORTER.oldestToNewest);

    triggerMessagesRefreshed();
  };

  ROOT.getSelectedChannel = function(){
    return selectedChannel;
  };
  
  // ------------
  // Message list
  // ------------
  
  ROOT.getMessageList = function(){
    if (!MSGS){
      return [];
    }

    var messages = FILE.getMessages(selectedChannel);
    var startIndex = messagesPerPage*(ROOT.getCurrentPage()-1);

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
  };
  
  // ----------
  // Pagination
  // ----------

  ROOT.setMessagesPerPage = function(amount){
    messagesPerPage = amount;
    triggerMessagesRefreshed();
  };

  ROOT.updateCurrentPage = function(action){
    switch(action){
      case "first": currentPage = 1; break;
      case "prev": currentPage = Math.max(1, currentPage-1); break;
      case "next": currentPage = Math.min(ROOT.getPageCount(), currentPage+1); break;
      case "last": currentPage = ROOT.getPageCount(); break;
    }

    triggerMessagesRefreshed();
  };

  ROOT.getCurrentPage = function(){
    var total = ROOT.getPageCount();

    if (currentPage > total && total > 0){
      currentPage = total;
    }

    return currentPage || 1;
  };

  ROOT.getPageCount = function(){
    return !MSGS ? 0 : (!messagesPerPage ? 1 : Math.ceil(MSGS.length/messagesPerPage));
  };
  
  // --------
  // Settings
  // --------
  
  ROOT.settings = {};
  
  var defineSettingProperty = (property, defaultValue) => {
    var name = "_"+property;
    
    Object.defineProperty(ROOT.settings, property, {
      get: (() => ROOT.settings[name]),
      set: (value => {
        ROOT.settings[name] = value;
        triggerMessagesRefreshed();
      })
    });
    
    ROOT.settings[name] = defaultValue;
  }
  // End
  return ROOT;
})();
