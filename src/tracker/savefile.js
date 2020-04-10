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
 *         m: <message content>, // only present if not empty
 *         f: <message flags>,   // only present if edited in which case it equals 1, deprecated (use 'te' instead),
 *         te: <edit timestamp>, // only present if edited,
 *         e: [ // omit for no embeds
 *           {
 *             url: <embed url>,
 *             type: <embed type>,
 *             t: <rich embed title>,      // only present if type == rich, and if not empty
 *             d: <rich embed description> // only present if type == rich, and if the embed has a simple description text
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
 *   },
 *   channelkeys: Set<channel id>,
 *   messagekeys: Set<message id>,
 *   freshmsgs: Set<message id> // only messages which were newly added to the savefile in the current session
 * }
 */

class SAVEFILE{
  constructor(parsedObj){
    var me = this;
    
    if (!SAVEFILE.isValid(parsedObj)){
      parsedObj = {
        meta: {},
        data: {}
      };
    }
    
    me.meta = parsedObj.meta;
    me.data = parsedObj.data;
    
    me.meta.users = me.meta.users || {};
    me.meta.userindex = me.meta.userindex || [];
    me.meta.servers = me.meta.servers || [];
    me.meta.channels = me.meta.channels || {};
    
    me.tmp = {
      userlookup: {},
      channelkeys: new Set(),
      messagekeys: new Set(),
      freshmsgs: new Set()
    }
  }
  
  static isValid(parsedObj){
    return parsedObj && typeof parsedObj.meta === "object" && typeof parsedObj.data === "object";
  }
  
  findOrRegisterUser(userId, userName){
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
  }
  
  findOrRegisterServer(serverName, serverType){
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
  }
  
  tryRegisterChannel(serverIndex, channelId, channelName){
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
  }
  
  addMessage(channelId, messageId, messageObject){
    var container = this.data[channelId] || (this.data[channelId] = {});
    var wasPresent = messageId in container;
    
    container[messageId] = messageObject;
    this.tmp.messagekeys.add(messageId);
    return !wasPresent;
  }
  
  convertToMessageObject(discordMessage){
    var obj = {
      u: this.findOrRegisterUser(discordMessage.author.id, discordMessage.author.username),
      t: discordMessage.timestamp.toDate().getTime()
    };
    
    if (discordMessage.content.length > 0){
      obj.m = discordMessage.content;
    }
    
    if (discordMessage.editedTimestamp !== null){
      obj.te = discordMessage.editedTimestamp.toDate().getTime();
    }
    
    if (discordMessage.embeds.length > 0){
      obj.e = discordMessage.embeds.map(embed => {
        let conv = {
          url: embed.url,
          type: embed.type
        };
        
        if (embed.type === "rich"){
          if (Array.isArray(embed.title) && embed.title.length === 1 && typeof embed.title[0] === "string"){
            conv.t = embed.title[0];
            
            if (Array.isArray(embed.description) && embed.description.length === 1 && typeof embed.description[0] === "string"){
              conv.d = embed.description[0];
            }
          }
        }
        
        return conv;
      });
    }
    
    if (discordMessage.attachments.length > 0){
      obj.a = discordMessage.attachments.map(attachment => ({
        url: attachment.url
      }));
    }
    
    return obj;
  }
  
  isMessageFresh(id){
    return this.tmp.freshmsgs.has(id);
  }
  
  addMessagesFromDiscord(channelId, discordMessageArray){
    var hasNewMessages = false;
    
    for(var discordMessage of discordMessageArray){
      if (discordMessage.state === "SENT" && this.addMessage(channelId, discordMessage.id, this.convertToMessageObject(discordMessage))){
        this.tmp.freshmsgs.add(discordMessage.id);
        hasNewMessages = true;
      }
    }
    
    return hasNewMessages;
  }
  
  countChannels(){
    return this.tmp.channelkeys.size;
  }
  
  countMessages(){
    return this.tmp.messagekeys.size;
  }
  
  combineWith(obj){
    var userMap = {};
    var shownError = false;
    
    for(var userId in obj.meta.users){
      userMap[obj.meta.userindex.findIndex(id => id == userId)] = this.findOrRegisterUser(userId, obj.meta.users[userId].name);
    }
    
    for(var channelId in obj.meta.channels){
      var oldServer = obj.meta.servers[obj.meta.channels[channelId].server];
      this.tryRegisterChannel(this.findOrRegisterServer(oldServer.name, oldServer.type), channelId, obj.meta.channels[channelId].name);
    }
    
    for(var channelId in obj.data){
      var oldChannel = obj.data[channelId];
      
      for(var messageId in oldChannel){
        var oldMessage = oldChannel[messageId];
        var oldUser = oldMessage.u;
        
        if (oldUser in userMap){
          oldMessage.u = userMap[oldUser];
          this.addMessage(channelId, messageId, oldMessage);
        }
        else{
          if (!shownError){
            shownError = true;
            alert("The uploaded archive appears to be corrupted, some messages will be skipped. See console for details.");
            
            console.error("User list:", obj.meta.users);
            console.error("User index:", obj.meta.userindex);
            console.error("Generated mapping:", userMap);
            console.error("Missing user for the following messages:");
          }
          
          console.error(oldMessage);
        }
      }
    }
  }
  
  toJson(){
    return JSON.stringify({
      "meta": this.meta,
      "data": this.data
    });
  }
}
