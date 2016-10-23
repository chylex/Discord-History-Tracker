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
 *         type: <"SERVER"|"DM">
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
 *         f: <message flags>, // bit 1 = edited, bit 2 = has user mentions (omit for no flags),
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

var SAVEFILE = function(){
  this.meta = {};
  this.meta.users = {};
  this.meta.userindex = [];
  this.meta.servers = [];
  this.meta.channels = {};
  
  this.data = {};
  
  this.tmp = {};
  this.tmp.userlookup = {};
};

SAVEFILE.prototype.findOrRegisterUser = function(userId, userName){
  if (!(userId in this.meta.users)){
    this.meta.users[userId] = {
      name: userName
    };
    
    this.meta.userindex.push(userId);
    return this.tmp.userlookup[userId] = this.meta.userindex.length-1;
  }
  else{
    return this.tmp.userlookup[userId];
  }
};

SAVEFILE.prototype.findOrRegisterServer = function(serverName, serverType){
  var index = this.meta.servers.findIndex(server => server.name === serverName && server.type === serverType);
  
  if (index === -1){
    this.meta.servers.push({
      name: serverName,
      type: serverType
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
      server: serverIndex,
      name: channelName
    };
    
    return true;
  }
};

SAVEFILE.prototype.toJson = function(){
  return JSON.stringify({
    meta: this.meta,
    data: this.data
  });
};
