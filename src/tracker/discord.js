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
          callback(match[1]);
        }
      });
    },
    
    /*
     * Returns internal React state object of an element.
     */
    getReactProps: function(ele){
      var key = Object.keys(ele || {}).find(key => key.startsWith("__reactInternalInstance"));
      return key ? ele[key]._currentElement.props : null;
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
        channelListEle = DOM.fcls("channels-wrap");
        channel = channelListEle.querySelector("[class|='wrapperSelectedText']").parentElement;
        
        if (!channel){
          return null;
        }
        else{
          var props = DISCORD.getReactProps(channel);
          
          if (!props){
            return null;
          }
          
          var channelObj = props.children.props.channel;

          obj = {
            "server": channelListEle.querySelector("header > span").innerHTML,
            "channel": channelObj.name,
            "id": channelObj.id,
            "type": "SERVER"
          };
        }
      }
      
      return obj.channel.length === 0 ? null : obj;
    },
    
    /*
     * Returns an array containing currently loaded messages.
     */
    getMessages: function(){
      var props = DISCORD.getReactProps(DOM.fcls("messages"));
      var array = props && props.children.find(ele => ele && ele.length);
      var messages = [];
      
      if (array){
        for(let obj of array){
          if (obj.props.messages){
            Array.prototype.push.apply(messages, obj.props.messages);
          }
        }
      }
      
      return messages;
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
      var wrap = DOM.fcls("channels-wrap");
      
      if (wrap.children[0].classList.contains("private-channels")){
        var nextChannel = DOM.fcls("selected", wrap).nextElementSibling;
        var classes = nextChannel && nextChannel.classList;

        if (nextChannel === null || !classes.contains("channel") || !classes.contains("private")){
          return false;
        }
        else{
          DOM.ftag("a", nextChannel).click();
          return true;
        }
      }
      else{
        var allChannels = wrap.querySelectorAll("[class|='containerDefault']");
        var nextChannel = null;
        
        for(var index = 0; index < allChannels.length-1; index++){
          if (allChannels[index].children[0].className.includes("wrapperSelectedText")){
            var next = allChannels[index+1];
            
            if (next.childElementCount > 0 && /wrapper([a-zA-Z]+?)Text/.test(next.children[0].className)){
              nextChannel = allChannels[index+1];
              break;
            }
          }
        }
        
        if (nextChannel === null){
          return false;
        }
        else{
          nextChannel.children[0].click();
          return true;
        }
      }
    }
  };
})();
