var DISCORD = (function(){
  var templateChannelServer;
  var templateChannelPrivate;
  var templateMessage;
  
  return {
    setup: function(){
      templateChannelServer = new TEMPLATE("<div data-channel='{id}'><strong class='name'>{name}</strong> <span class='msgcount'>({msgcount})</span><br><span class='server'>{server.name} ({server.type})</span></div>");
      templateChannelPrivate = new TEMPLATE("<div data-channel='{id}'><strong class='name'>{name}</strong> <span class='msgcount'>({msgcount})</span><br><span class='server'>({server.type})</span></div>");
      templateMessage = new TEMPLATE("<div><h2><strong class='username'>{user.name}</strong><span class='time'>{timestamp}</span></h2><div class='message'>{contents}</div></div>");
    },
    
    getChannelHTML: function(channel){
      return (channel.server.type === "SERVER" ? templateChannelServer : templateChannelPrivate).apply(channel, (property, value) => {
        if (property === "server.type"){
          switch(value){
            case "SERVER": return "server";
            case "GROUP": return "group";
          }
        }
      });
    },
    
    getMessageHTML: function(message){
      return templateMessage.apply(message, (property, value) => {
        if (property === "timestamp"){
          var date = new Date(value);
          return date.toLocaleDateString()+", "+date.toLocaleTimeString();
        }
      });
    }
  };
})();
