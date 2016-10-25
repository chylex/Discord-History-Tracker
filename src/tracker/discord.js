var DISCORD = (function(){
  var regexMessageRequest = /\/channels\/(\d+)\/messages[^a-z]/;
  
  return {
    /*
     * Sets up a callback hook to trigger whenever a message request returns a response (the callback is given the channel ID and message array).
     */
    setupMessageRequestHook: function(callback){
      HOOKS.onAjaxResponse(function(args, req){
        var match = args[1].match(regexMessageRequest);
        
        if (match){
          var channel = match[1];
          var messages = JSON.parse(req.response);
          
          callback(channel, messages);
        }
      });
    },
    
    /*
     * Returns an object containing the selected server name, selected channel name and ID, and the object type.
     * For types DM and GROUP, the server and channel names are identical.
     * For SERVER type, the channel has to be in view, otherwise Discord unloads it.
     */
    getSelectedChannel: function(){
      var obj;
      
      var channelListEle = DOM.cls("private-channels")[0];
      var channel;
      
      if (channelListEle){
        channel = DOM.cls("selected", channelListEle)[0];
        
        if (!channel || !channel.classList.contains("private")){
          return null;
        }
        else{
          var linkSplit = DOM.tag("a", channel)[0].getAttribute("href").split("/");
          var name = [].find.call(DOM.cls("channel-name", channel)[0].childNodes, node => node.nodeType === Node.TEXT_NODE).nodeValue;
          
          obj = {
            server: name,
            channel: name,
            id: linkSplit[linkSplit.length-1],
            type: DOM.cls("status", channel).length ? "DM" : "GROUP"
          };
        }
      }
      else{
        channelListEle = DOM.cls("guild-channels")[0];
        channel = channelListEle && DOM.cls("selected", channelListEle)[0];
        
        if (!channel){
          return null;
        }
        else{
          var linkSplit = DOM.tag("a", channel)[0].getAttribute("href").split("/");

          obj = {
            server: DOM.tag("span", DOM.cls("guild-header")[0])[0].innerHTML,
            channel: DOM.cls("channel-name", DOM.cls("selected", channelListEle)[0])[0].innerHTML,
            id: linkSplit[linkSplit.length-1],
            type: "SERVER"
          };
        }
      }
      
      return obj.channel.length === 0 ? null : obj;
    },
    
    /*
     * Returns true if the message column is visible.
     */
    isInMessageView: function(){
      return DOM.cls("messages").length > 0;
    },
    
    /*
     * Returns true if there are more messages available.
     */
    hasMoreMessages: function(){
      return DOM.cls("messages")[0].children[0].classList.contains("has-more");
    },
    
    /*
     * Forces the message column to scroll all the way up to load older messages.
     */
    loadOlderMessages: function(){
      DOM.cls("messages")[0].scrollTop = 0;
    },
    
    /*
     * Selects the next text channel and returns true, otherwise returns false if there are no more channels.
     */
    selectNextTextChannel: function(){
      var nextChannel = DOM.cls("selected", DOM.cls("channels-wrap")[0])[0].nextElementSibling;
      var classes = nextChannel && nextChannel.classList;
      
      if (nextChannel === null || !classes.contains("channel") || !(classes.contains("private") || classes.contains("channel-text"))){
        return false;
      }
      else{
        DOM.tag("a", nextChannel)[0].click();
        return true;
      }
    }
  };
})();
