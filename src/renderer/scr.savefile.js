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

SAVEFILE.prototype.getChannelList = function(){
  return Object.keys(this.meta.channels).map(key => ({
    id: key,
    name: this.meta.channels[key].name,
    server: this.getServer(this.meta.channels[key].server),
    msgcount: Object.keys(this.data[key]).length || 0
  }));
};

SAVEFILE.prototype.getUser = function(index){
  return this.meta.users[this.meta.userindex[index]] || { name: "&lt;unknown&gt;" };
};

SAVEFILE.prototype.getChannelMessageObject = function(channel){
  return this.data[channel] || [];
};

SAVEFILE.prototype.getMessage = function(message){
  return {
    user: this.getUser(message.u),
    timestamp: message.t,
    contents: message.m
  };
};
