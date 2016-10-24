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
     */
    getSelectedChannel: function(){
      var obj;
      var channelListEle = DOM.cls("private-channels");
      
      if (channelListEle.length !== 0){
        var channel = DOM.cls("selected", channelListEle[0])[0];
        
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
        channelListEle = DOM.cls("guild-channels");
        
        if (channelListEle.length === 0){
          return null;
        }
        else{
          var linkSplit = DOM.tag("a", channel)[0].getAttribute("href").split("/");
          var name = DOM.tag("span", DOM.cls("guild-header")[0]).innerHTML;

          obj = {
            server: DOM.tag("span", DOM.cls("guild-header")[0])[0].innerHTML,
            channel: DOM.cls("channel-name", DOM.cls("selected", channelListEle[0])[0])[0].innerHTML,
            id: linkSplit[linkSplit.length-1],
            type: "SERVER"
          };
        }
      }
      
      return obj.channel.length === 0 ? null : obj;
    }
  };
})();
