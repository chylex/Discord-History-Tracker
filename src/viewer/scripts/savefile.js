class SAVEFILE{
  constructor(parsedObj){
    var me = this;
    
    me.meta = parsedObj.meta;
    me.data = parsedObj.data;
    
    me.meta.users = me.meta.users || {};
    me.meta.userindex = me.meta.userindex || [];
    me.meta.servers = me.meta.servers || [];
    me.meta.channels = me.meta.channels || {};
  };

  static isValid(parsedObj){
    return parsedObj && typeof parsedObj.meta === "object" && typeof parsedObj.data === "object";
  };

  getServer(index){
    return this.meta.servers[index] || { "name": "&lt;unknown&gt;", "type": "ERROR" };
  }

  getChannels(){
    return this.meta.channels;
  }

  getChannelById(channel){
    return this.meta.channels[channel] || { "id": channel, "name": channel };
  }

  getUsers(){
    return this.meta.users;
  }

  getUser(index){
    return this.meta.users[this.meta.userindex[index]] || { "name": "&lt;unknown&gt;" };
  }

  getUserId(index){
    return this.meta.userindex[index];
  }

  getUserById(user){
    return this.meta.users[user] || { "name": user };
  }

  getUserIndex(user){
    return this.meta.userindex.indexOf(user);
  }

  getMessages(channel){
    return this.data[channel] || {};
  }

  filterToJson(filterFunction){
    var newMeta = JSON.parse(JSON.stringify(this.meta));
    var newData = {};
    
    for(let channel of Object.keys(this.getChannels())){
      var messages = this.getMessages(channel);
      var retained = {};
      
      for(let key of Object.keys(messages)){
        var message = messages[key];
        
        if (filterFunction(message)){
          retained[key] = message;
        }
      }
      
      if (Object.keys(retained).length > 0){
        newData[channel] = retained;
      }
      else{
        delete newMeta.channels[channel];
      }
    }
    
    return JSON.stringify({
      "meta": newMeta,
      "data": newData
    });
  }
}
