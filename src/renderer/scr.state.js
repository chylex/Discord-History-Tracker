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
  var messagesByUser;
  
  // ----------------------------------
  // Channel and message refresh events
  // ----------------------------------
  
  var eventOnChannelsRefreshed;
  var eventOnMessagesRefreshed;
  var eventOnUsersRefreshed;
  
  var triggerChannelsRefreshed = function(){
    eventOnChannelsRefreshed && eventOnChannelsRefreshed(ROOT.getChannelList());
  };
  
  var triggerMessagesRefreshed = function(){
    eventOnMessagesRefreshed && eventOnMessagesRefreshed(ROOT.getMessageList());
  };
  
  var triggerUsersRefreshed = function(){
    eventOnUsersRefreshed && eventOnUsersRefreshed(ROOT.getUserList());
  };

  ROOT.onChannelsRefreshed = function(callback){
    eventOnChannelsRefreshed = callback;
  };
    
  ROOT.onMessagesRefreshed = function(callback){
    eventOnMessagesRefreshed = callback;
  };
  
  ROOT.onUsersRefreshed = function(callback){
    eventOnUsersRefreshed = callback;
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
    triggerUsersRefreshed();
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
    var userindex = messagesByUser ? FILE.meta.userindex.indexOf(messagesByUser) : -1;

    return Object.keys(channels).map(key => ({
      "id": key,
      "name": channels[key].name,
      "server": FILE.getServer(channels[key].server),
      "msgcount": FILE.getFilteredMessageCount(key, userindex)
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
  ROOT.getFilteredMessageList = function(){
    if (!messagesByUser) {
      return MSGS;
    }

    var pos = FILE.meta.userindex.indexOf(messagesByUser);
    var messages = FILE.getMessages(selectedChannel);
    return MSGS.filter(key =>  {
      var message = messages[key];
      return message.u == pos;
    });
  };

  
  ROOT.getMessageList = function(){
    if (!MSGS){
      return [];
    }

    var messages = FILE.getMessages(selectedChannel);
    var startIndex = messagesPerPage*(ROOT.getCurrentPage()-1);

    return ROOT.getFilteredMessageList().slice(startIndex, !messagesPerPage ? undefined : startIndex+messagesPerPage).map(key => {
      var message = messages[key];

      return {
        "user": FILE.getUser(message.u),
        "timestamp": message.t,
        "contents": message.m,
        "embeds": message.e,
        "attachments": message.a,
        "edited": (message.f&1) === 1
      };
    });
  };
  
  ROOT.getUserList = function(){
    if (!FILE){
      return [];
    }

    return FILE.meta.users;
  };

  // ----------
  // Filtering
  // ----------

  ROOT.setMessagesByUser = function(key){
    currentPage = 1;
    messagesByUser = key;
    triggerChannelsRefreshed();
    triggerMessagesRefreshed();
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
    return !MSGS ? 0 : (!messagesPerPage ? 1 : Math.ceil(ROOT.getFilteredMessageList().length/messagesPerPage));
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
  
  defineSettingProperty("enableImagePreviews", true);
  defineSettingProperty("enableFormatting", true);
  
  // End
  return ROOT;
})();
