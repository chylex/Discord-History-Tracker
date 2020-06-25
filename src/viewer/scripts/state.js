var STATE = (function(){
  var ROOT = {};
  
  // ---------------
  // State variables
  // ---------------
  
  var FILE;
  var MSGS;
  
  var uploadedFileName;
  var filterFunction;
  var selectedChannel;
  var currentPage;
  var messagesPerPage;
  
  // ----------------------------------
  // Channel and message refresh events
  // ----------------------------------
  
  var eventOnChannelsRefreshed;
  var eventOnMessagesRefreshed;
  var eventOnUsersRefreshed;
  
  var triggerChannelsRefreshed = function(selectedChannel){
    eventOnChannelsRefreshed && eventOnChannelsRefreshed(ROOT.getChannelList(), selectedChannel);
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
    
    selectedChannel = null;
    currentPage = 1;
    
    triggerUsersRefreshed();
    triggerChannelsRefreshed();
    triggerMessagesRefreshed();
  };
  
  ROOT.setUploadedFileName = function(name){
    uploadedFileName = name;
  };

  ROOT.getChannelName = function(channel){
    return FILE.getChannelById(channel).name;
  };
  
  ROOT.getUserName = function(user){
    return FILE.getUserById(user).name;
  };
  
  ROOT.getUserTag = function(user){
    return FILE.getUserById(user).tag;
  };
  
  // --------------------------
  // Channel list and selection
  // --------------------------
  
  var getFilteredMessageKeys = function(channel){
    var messages = FILE.getMessages(channel);
    var keys = Object.keys(messages);
    
    if (filterFunction){
      keys = keys.filter(key => filterFunction(messages[key]));
    }
    
    return keys;
  };

  ROOT.getChannelList = function(){
    if (!FILE){
      return [];
    }
    
    var channels = FILE.getChannels();

    return Object.keys(channels).map(key => ({
      "id": key,
      "name": channels[key].name,
      "server": FILE.getServer(channels[key].server),
      "msgcount": getFilteredMessageKeys(key).length,
      "topic": channels[key].topic || "",
      "nsfw": channels[key].nsfw || false,
      "position": channels[key].position || -1
    })).sort((ac, bc) => {
      var as = ac.server;
      var bs = bc.server;
      
      return as.type.localeCompare(bs.type, "en") ||
             as.name.toLocaleLowerCase().localeCompare(bs.name.toLocaleLowerCase(), undefined, { numeric: true }) ||
             ac.position - bc.position ||
             ac.name.toLocaleLowerCase().localeCompare(bc.name.toLocaleLowerCase(), undefined, { numeric: true });
    });
  };

  ROOT.selectChannel = function(channel){
    currentPage = 1;
    selectedChannel = channel;
    
    MSGS = getFilteredMessageKeys(channel).sort(PROCESSOR.SORTER.oldestToNewest);
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
      var user = FILE.getUser(message.u);
      var avatar = user.avatar ? { id: FILE.getUserId(message.u), path: user.avatar } : null;

      return {
        "user": user,
        "avatar": avatar,
        "timestamp": message.t,
        "contents": ("m" in message) ? message.m : null,
        "embeds": message.e,
        "attachments": message.a,
        "edit": ("te" in message) ? message.te : (message.f & 1) === 1,
        "jump": key
      };
    });
  };
  
  ROOT.navigateToMessage = function(id){
    if (!MSGS){
      return 0;
    }
    
    var index = MSGS.indexOf(id);
    
    if (index == -1){
      return 0;
    }
    
    currentPage = Math.max(1, Math.min(ROOT.getPageCount(), 1 + Math.floor(index / messagesPerPage)));
    triggerMessagesRefreshed();
    return index % messagesPerPage;
  };

  // ----------
  // Filtering
  // ----------
  
  ROOT.hasActiveFilter = false;
  
  ROOT.setActiveFilter = function(filter){
    switch(filter ? filter.type : ""){
      case "user":
        filterFunction = PROCESSOR.FILTER.byUser(FILE.getUserIndex(filter.value));
        break;
        
      case "contents":
        filterFunction = PROCESSOR.FILTER.byContents(filter.value);
        break;
        
      case "withimages":
        filterFunction = PROCESSOR.FILTER.withImages();
        break;
        
      case "withdownloads":
        filterFunction = PROCESSOR.FILTER.withDownloads();
        break;
        
      case "edited":
        filterFunction = PROCESSOR.FILTER.isEdited();
        break;

      default:
        filterFunction = null;
        break;
    }
    
    ROOT.hasActiveFilter = filterFunction != null;
    
    triggerChannelsRefreshed(selectedChannel);
    
    if (selectedChannel){
      ROOT.selectChannel(selectedChannel); // resets current page and updates messages
    }
  };
  
  ROOT.saveFilteredMessages = function(){
    var saveFileName = "dht-filtered.txt";
    
    if (uploadedFileName){
      if (uploadedFileName.includes("filtered")){
        saveFileName = uploadedFileName;
      }
      else{
        saveFileName = uploadedFileName.replace(".", "-filtered.");
      }
    }
    
    DOM.downloadTextFile(saveFileName, FILE.filterToJson(filterFunction));
  };
  
  // -----
  // Users
  // -----
  
  ROOT.getUserList = function(){
    return FILE ? FILE.getUsers() : [];
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
      
      case "pick":
        var page = parseInt(prompt("Select page:", currentPage), 10);
        
        if (!page && page !== 0){
          return;
        }
        
        currentPage = Math.max(1, Math.min(ROOT.getPageCount(), page));
        break;
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
  
  var getStorageItem = (property) => {
    try{
      return localStorage.getItem(property);
    }catch(e){
      console.error(e);
      return null;
    }
  };
  
  var setStorageItem = (property, value) => {
    try{
      localStorage.setItem(property, value);
    }catch(e){
      console.error(e);
    }
  };
  
  var defineSettingProperty = (property, defaultValue, storageToValue) => {
    var name = "_"+property;
    
    Object.defineProperty(ROOT.settings, property, {
      get: (() => ROOT.settings[name]),
      set: (value => {
        ROOT.settings[name] = value;
        triggerMessagesRefreshed();
        setStorageItem(property, value);
      })
    });
    
    var stored = getStorageItem(property);
    
    if (stored !== null){
      stored = storageToValue(stored);
    }
    
    ROOT.settings[name] = stored === null ? defaultValue : stored;
  };
  
  var fromBooleanString = (value) => {
    if (value === "true") return true;
    if (value === "false") return false;
    return null;
  };
  
  defineSettingProperty("enableImagePreviews", true, fromBooleanString);
  defineSettingProperty("enableFormatting", true, fromBooleanString);
  defineSettingProperty("enableUserAvatars", true, fromBooleanString);
  defineSettingProperty("enableAnimatedEmoji", true, fromBooleanString);
  
  // End
  return ROOT;
})();
