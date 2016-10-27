var SAVEFILE = function(parsedObj){
  this.meta = parsedObj.meta;
  this.meta.users = this.meta.users || {};
  this.meta.userindex = this.meta.userindex || [];
  this.meta.servers = this.meta.servers || [];
  this.meta.channels = this.meta.channels || {};
  
  this.data = parsedObj.data;
};

SAVEFILE.isValid = function(parsedObj){
  return parsedObj && typeof parsedObj.meta === "object" && typeof parsedObj.data === "object";
};

SAVEFILE.prototype.getServer = function(index){
  return this.meta.servers[index] || {
    name: "&lt;unknown&gt;",
    type: "ERROR"
  };
};

SAVEFILE.prototype.getChannels = function(){
  return this.meta.channels;
};

SAVEFILE.prototype.getChannelById = function(channel){
  return this.meta.channels[channel] || { id: channel, name: channel };
};

SAVEFILE.prototype.getUser = function(index){
  return this.meta.users[this.meta.userindex[index]] || { name: "&lt;unknown&gt;" };
};

SAVEFILE.prototype.getUserById = function(user){
  return this.meta.users[user] || { name: user };
};

SAVEFILE.prototype.getMessageCount = function(channel){
  return Object.keys(this.data[channel]).length || 0;
};

SAVEFILE.prototype.getMessages = function(channel){
  return this.data[channel] || [];
};
