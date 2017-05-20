var DISCORD = (function(){
  var regexMessageRequest = /\/channels\/(\d+)\/messages[^a-z]/;
  
  return {
    /*
     * Sets up a callback hook to trigger whenever a message request returns a response (the callback is given the channel ID and message array).
     */
    setupMessageRequestHook: function(callback){
      HOOKS.onAjaxResponse((args, req) => {
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
      
      var channelListEle = DOM.fcls("private-channels");
      var channel;
      
      if (channelListEle){
        channel = DOM.fcls("selected", channelListEle);
        
        if (!channel || !channel.classList.contains("private")){
          return null;
        }
        else{
          var linkSplit = DOM.ftag("a", channel).href.split("/");
          var name = [].find.call(DOM.fcls("channel-name", channel).childNodes, node => node.nodeType === Node.TEXT_NODE).nodeValue;
          
          obj = {
            "server": name,
            "channel": name,
            "id": linkSplit[linkSplit.length-1],
            "type": DOM.cls("status", channel).length ? "DM" : "GROUP"
          };
        }
      }
      else{
        channelListEle = DOM.fcls("guild-channels");
        channel = channelListEle && DOM.fcls("selected", channelListEle);
        
        if (!channel){
          return null;
        }
        else{
          var linkSplit = DOM.ftag("a", channel).href.split("/");

          obj = {
            "server": DOM.ftag("span", DOM.fcls("guild-header")).innerHTML,
            "channel": DOM.fcls("channel-name", DOM.fcls("selected", channelListEle)).innerHTML,
            "id": linkSplit[linkSplit.length-1],
            "type": "SERVER"
          };
        }
      }
      
      return obj.channel.length === 0 ? null : obj;
    },
    
    /*
     * Returns true if the message column is visible.
     */
    isInMessageView: (_) => DOM.cls("messages").length > 0,
    
    /*
     * Returns true if there are more messages available.
     */
    hasMoreMessages: (_) => DOM.fcls("messages").children[0].classList.contains("has-more"),
    
    /*
     * Forces the message column to scroll all the way up to load older messages.
     */
    loadOlderMessages: (_) => DOM.fcls("messages").scrollTop = 0,
    
    /*
     * Selects the next text channel and returns true, otherwise returns false if there are no more channels.
     */
    selectNextTextChannel: function(){
      var nextChannel = DOM.fcls("selected", DOM.fcls("channels-wrap")).nextElementSibling;
      var classes = nextChannel && nextChannel.classList;
      
      if (nextChannel === null || !classes.contains("channel") || !(classes.contains("private") || classes.contains("channel-text"))){
        return false;
      }
      else{
        DOM.ftag("a", nextChannel).click();
        return true;
      }
    }
  };
})();
