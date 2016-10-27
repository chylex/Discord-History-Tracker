var DISCORD = (function(){
  var templateChannelServer;
  var templateChannelPrivate;
  var templateMessage;
  
  return {
    setup: function(){
      templateChannelServer = new TEMPLATE([
        "<div data-channel='{id}'>",
        "<div class='info'><strong class='name'>#{name}</strong><span class='msgcount'>{msgcount}</span></div>",
        "<span class='server'>{server.name} ({server.type})</span>",
        "</div>"
      ].join(""));
      
      templateChannelPrivate = new TEMPLATE([
        "<div data-channel='{id}'>",
        "<div class='info'><strong class='name'>{name}</strong><span class='msgcount'>{msgcount}</span></div>",
        "<span class='server'>({server.type})</span>",
        "</div>"
      ].join(""));
      
      templateMessage = new TEMPLATE([
        "<div>",
        "<h2><strong class='username'>{user.name}</strong><span class='time'>{timestamp}</span></h2>",
        "<div class='message'>{contents}</div>",
        "</div>"
      ].join(""));
    },
    
    getChannelHTML: function(channel){
      return (channel.server.type === "SERVER" ? templateChannelServer : templateChannelPrivate).apply(channel, (property, value) => {
        if (property === "server.type"){
          switch(value){
            case "SERVER": return "server";
            case "GROUP": return "group";
            case "DM": return "user";
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
