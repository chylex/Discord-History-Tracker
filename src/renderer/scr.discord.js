var DISCORD = (function(){
  var REGEX = {
    formatBold: /\*\*([\s\S]+?)\*\*(?!\*)/g,
    formatItalic: /\*([\s\S]+?)\*(?!\*)/g,
    formatUnderline: /__([\s\S]+?)__(?!_)/g,
    formatStrike: /~~([\s\S]+?)~~(?!~)/g,
    formatCodeInline: /`+\s*([\s\S]*?[^`])\s*\1(?!`)/g,
    formatCodeBlock: /```(?:([A-z0-9\-]+?)\n+)?\n*([^]+?)\n*```/g,
    formatUrl: /<?(\b(?:https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])>?/ig,
    mentionRole: /&lt;@&(\d+?)&gt;/g,
    mentionUser: /&lt;@!?(\d+?)&gt;/g,
    mentionChannel: /&lt;#(\d+?)&gt;/g
  };
  
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
        else if (property === "contents"){
          if (value.length === 0){
            return "";
          }
          
          var processed = DOM.escapeHTML(value)
            .replace(REGEX.formatBold, "<b>$1</b>")
            .replace(REGEX.formatItalic, "<i>$1</i>")
            .replace(REGEX.formatUnderline, "<u>$1</u>")
            .replace(REGEX.formatStrike, "<s>$1</s>")
            .replace(REGEX.formatCodeBlock, "<code style='display:block'>$2</code>")
            .replace(REGEX.formatCodeInline, "<code>$1</code>")
            .replace(REGEX.formatUrl, "<a href='$1' target='_blank' rel='noreferrer'>$1</a>")
            .replace(REGEX.mentionChannel, (full, match) => "<span class='link mention-chat'>#"+STATE.getChannelName(match)+"</span>")
            .replace(REGEX.mentionUser, (full, match) => "<span class='link mention-user'>@"+STATE.getUserName(match)+"</span>");
          
          return "<p>"+processed+"</p>";
        }
      });
    }
  };
})();
