var SAVEFILE = function(parsedObj){
  var me = this;
  
  me.meta = parsedObj.meta;
  me.meta.users = me.meta.users || {};
  me.meta.userindex = me.meta.userindex || [];
  me.meta.servers = me.meta.servers || [];
  me.meta.channels = me.meta.channels || {};
  
  me.data = parsedObj.data;
};

SAVEFILE.isValid = function(parsedObj){
  return parsedObj && typeof parsedObj.meta === "object" && typeof parsedObj.data === "object";
};

SAVEFILE.prototype.getServer = function(index){
  return this.meta.servers[index] || { "name": "&lt;unknown&gt;", "type": "ERROR" };
};

SAVEFILE.prototype.getChannels = function(){
  return this.meta.channels;
};

SAVEFILE.prototype.getChannelById = function(channel){
  return this.meta.channels[channel] || { "id": channel, "name": channel };
};

SAVEFILE.prototype.getUser = function(index){
  return this.meta.users[this.meta.userindex[index]] || { "name": "&lt;unknown&gt;" };
};

SAVEFILE.prototype.getUserById = function(user){
  return this.meta.users[user] || { "name": user };
};

SAVEFILE.prototype.getMessageCount = function(channel){
  return Object.keys(this.data[channel]).length || 0;
};

SAVEFILE.prototype.getAllMessages = function(){
  var messages = {};
  
  UTILS.forEachValue(this.data, messageObject => {
    UTILS.combineObjects(messages, messageObject);
  });
  
  return messages;
};

SAVEFILE.prototype.getMessages = function(channel){
  return this.data[channel] || [];
};
