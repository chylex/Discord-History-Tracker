/*
 * SAVEFILE STRUCTURE
 * ==================
 *
 * {
 *   meta: {
 *     users: {
 *       <discord user id>: {
 *         name: <user name>
 *       }, ...
 *     },
 *
 *     // the user index is an array of discord user ids,
 *     // these indexes are used in the message objects to save space
 *     userindex: [
 *       <discord user id>, ...
 *     ],
 *
 *     servers: [
 *       {
 *         name: <server name>,
 *         type: <"SERVER"|"GROUP"|DM">
 *       }, ...
 *     ],
 *
 *     channels: {
 *       <discord channel id>: {
 *         server: <server index in the meta.servers array>,
 *         name: <channel name>
 *       }, ...
 *     }
 *   },
 *
 *   data: {
 *     <discord channel id>: {
 *       <discord message id>: {
 *         u: <user index of the sender>,
 *         t: <message timestamp>,
 *         m: <message content>,
 *         f: <message flags>, // bit 1 = edited (omit for no flags),
 *         e: [ // omit for no embeds
 *           {
 *             url: <embed url>,
 *             type: <embed type>
 *           }, ...
 *         ],
 *         a: [ // omit for no attachments
 *           {
 *             url: <attachment url>
 *           }, ...
 *         ]
 *       }, ...
 *     }, ...
 *   }
 * }
 *
 *
 * TEMPORARY OBJECT STRUCTURE
 * ==========================
 *
 * {
 *   userlookup: {
 *     <discord user id>: <user index in the meta.userindex array>
 *   }
 * }
 */

var SAVEFILE = function(parsedObj){
  var me = this;
  
  if (SAVEFILE.isValid(parsedObj)){
    me.meta = parsedObj.meta;
    me.meta.users = me.meta.users || {};
    me.meta.userindex = me.meta.userindex || [];
    me.meta.servers = me.meta.servers || [];
    me.meta.channels = me.meta.channels || {};
    
    me.data = parsedObj.data;
  }
  else{
    me.meta = {};
    me.meta.users = {};
    me.meta.userindex = [];
    me.meta.servers = [];
    me.meta.channels = {};
    
    me.data = {};
  }
  
  me.tmp = {};
  me.tmp.userlookup = {};
  me.tmp.channelkeys = new Set();
  me.tmp.messagekeys = new Set();
  me.tmp.freshmsgs = new Set();
};

SAVEFILE.isValid = function(parsedObj){
  return parsedObj && typeof parsedObj.meta === "object" && typeof parsedObj.data === "object";
};

SAVEFILE.prototype.findOrRegisterUser = function(userId, userName){
  if (!(userId in this.meta.users)){
    this.meta.users[userId] = {
      "name": userName
    };
    
    this.meta.userindex.push(userId);
    return this.tmp.userlookup[userId] = this.meta.userindex.length-1;
  }
  else if (!(userId in this.tmp.userlookup)){
    return this.tmp.userlookup[userId] = this.meta.userindex.findIndex(id => id == userId);
  }
  else{
    return this.tmp.userlookup[userId];
  }
};

SAVEFILE.prototype.findOrRegisterServer = function(serverName, serverType){
  var index = this.meta.servers.findIndex(server => server.name === serverName && server.type === serverType);
  
  if (index === -1){
    this.meta.servers.push({
      "name": serverName,
      "type": serverType
    });
    
    return this.meta.servers.length-1;
  }
  else{
    return index;
  }
};

SAVEFILE.prototype.tryRegisterChannel = function(serverIndex, channelId, channelName){
  if (!this.meta.servers[serverIndex]){
    return undefined;
  }
  else if (channelId in this.meta.channels){
    return false;
  }
  else{
    this.meta.channels[channelId] = {
      "server": serverIndex,
      "name": channelName
    };
    
    this.tmp.channelkeys.add(channelId);
    return true;
  }
};

SAVEFILE.prototype.addMessage = function(channelId, messageId, messageObject){
  var container = this.data[channelId] || (this.data[channelId] = {});
  var wasPresent = messageId in container;
  
  container[messageId] = messageObject;
  this.tmp.messagekeys.add(messageId);
  return !wasPresent;
};

SAVEFILE.prototype.convertToMessageObject = function(discordMessage){
  var obj = {
    u: this.findOrRegisterUser(discordMessage.author.id, discordMessage.author.username),
    t: +discordMessage.timestamp.toDate(),
    m: discordMessage.content
  };
  
  if (discordMessage.editedTimestamp !== null){
    obj.f = 1; // rewrite as bit flag if needed later
  }
  // Added description and title to embed array - Useful for archviing PokeHuntr bot messages.
  if (discordMessage.embeds.length > 0){
    obj.e = discordMessage.embeds.map(embed => ({
      "url": embed.url,
      "type": embed.type,
      "description":embed.description,
      "title":embed.title
    }));
  }
  
  if (discordMessage.attachments.length > 0){
    obj.a = discordMessage.attachments.map(attachment => ({
      "url": attachment.url
    }));
  }
  
  return obj;
};

SAVEFILE.prototype.isMessageFresh = function(id){
  return this.tmp.freshmsgs.has(id);
};

SAVEFILE.prototype.addMessagesFromDiscord = function(channelId, discordMessageArray){
  var hasNewMessages = false;
  
  for(var discordMessage of discordMessageArray){
    if (this.addMessage(channelId, discordMessage.id, this.convertToMessageObject(discordMessage))){
      this.tmp.freshmsgs.add(discordMessage.id);
      hasNewMessages = true;
    }
  }
  
  return hasNewMessages;
};

SAVEFILE.prototype.countChannels = function(){
  return this.tmp.channelkeys.size;
};

SAVEFILE.prototype.countMessages = function(){
  return this.tmp.messagekeys.size;
};

SAVEFILE.prototype.combineWith = function(obj){
  for(var userId in obj.meta.users){
    this.findOrRegisterUser(userId, obj.meta.users[userId].name);
  }
  
  for(var channelId in obj.meta.channels){
    var oldServer = obj.meta.servers[obj.meta.channels[channelId].server];
    this.tryRegisterChannel(this.findOrRegisterServer(oldServer.name, oldServer.type), channelId, obj.meta.channels[channelId].name);
  }
  
  for(var channelId in obj.data){
    for(var messageId in obj.data[channelId]){
      this.addMessage(channelId, messageId, obj.data[channelId][messageId]);
    }
  }
};

SAVEFILE.prototype.toJson = function(){
  return JSON.stringify({
    "meta": this.meta,
    "data": this.data
  });
};
