var DISCORD = (function(){
  var getTopMessageViewElement = function(){
    let view = DOM.queryReactClass("messages");
    return view && view.children.length && view.children[0];
  };
  
  var observerTimer = 0, waitingForCleanup = 0;
  
  return {
    /*
     * Sets up a callback hook to trigger whenever the list of messages is updated. The callback is given a boolean value that is true if there are more messages to load.
     */
    setupMessageUpdateCallback: function(callback){
      var onTimerFinished = function(){
        let topEle = getTopMessageViewElement();
        
        if (!topEle){
          restartTimer(500);
        }
        else if (!topEle.getAttribute("class").includes("loadingMore-")){
          let messages = DOM.queryReactClass("messages").children.length;
          
          if (messages < 100){
            waitingForCleanup = 0;
          }
          
          if (waitingForCleanup > 0){
            --waitingForCleanup;
            restartTimer(750);
          }
          else{
            if (messages > 300){
              waitingForCleanup = 6;
              
              DOM.setTimer(() => {
                let view = DOM.queryReactClass("messages");
                view.scrollTop = view.scrollHeight/2;
              }, 1);
            }
            
            callback(topEle.getAttribute("class").includes("hasMore-"));
            restartTimer(200);
          }
        }
        else{
          restartTimer(25);
        }
      };
      
      var restartTimer = function(delay){
        observerTimer = DOM.setTimer(onTimerFinished, delay);
      };
      
      onTimerFinished();
      window.DHT_ON_UNLOAD.push(() => window.clearInterval(observerTimer));
    },
    
    /*
     * Returns internal React state object of an element.
     */
    getReactProps: function(ele){
      var key = Object.keys(ele || {}).find(key => key.startsWith("__reactInternalInstance"));
      return key ? ele[key].memoizedProps : null;
    },
    
    /*
     * Returns an object containing the selected server name, selected channel name and ID, and the object type.
     * For types DM and GROUP, the server and channel names are identical.
     * For SERVER type, the channel has to be in view, otherwise Discord unloads it.
     */
    getSelectedChannel: function(){
      try{
        var obj;
        var channelListEle = document.querySelector("[class|='privateChannels']");
        
        if (channelListEle){
          var channel = DOM.queryReactClass("selected", channelListEle);
          
          if (!channel){
            return null;
          }
          
          var linkSplit = channel.querySelector("a[href*='/@me/']").href.split("/");
          var link = linkSplit[linkSplit.length-1];
          
          if (!(/^\d+$/.test(link))){
            return null;
          }
          
          var name = Array.prototype.find.call(channel.querySelector("[class|='name']").childNodes, node => node.nodeType === Node.TEXT_NODE).nodeValue;
          
          obj = {
            "server": name,
            "channel": name,
            "id": link,
            "type": !!DOM.queryReactClass("status", channel) ? "DM" : "GROUP"
          };
        }
        else{
          channelListEle = document.querySelector("[class|='channels']");
          
          var channel = channelListEle.querySelector("[class|='wrapperSelectedText']").parentElement;
          var props = DISCORD.getReactProps(channel);
          
          if (!props){
            return null;
          }
          
          var channelObj = props.children.props.channel;
          
          if (!channelObj){
            return null;
          }
          
          obj = {
            "server": channelListEle.querySelector("header > span").innerHTML,
            "channel": channelObj.name,
            "id": channelObj.id,
            "type": "SERVER"
          };
        }
        
        return obj.channel.length === 0 ? null : obj;
      }catch(e){
        return null;
      }
    },
    
    /*
     * Returns an array containing currently loaded messages.
     */
    getMessages: function(){
      var props = DISCORD.getReactProps(DOM.queryReactClass("messages"));
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
     * Returns true if the message view is visible.
     */
    isInMessageView: () => !!DOM.queryReactClass("messages"),
    
    /*
     * Returns true if there are more messages available or if they're still loading.
     */
    hasMoreMessages: function(){
      let classes = getTopMessageViewElement().getAttribute("class");
      return classes.includes("hasMore-") || classes.includes("loadingMore-");
    },
    
    /*
     * Forces the message view to load older messages by scrolling all the way up.
     */
    loadOlderMessages: function(){
      let view = DOM.queryReactClass("messages");
      view.scrollTop = view.scrollHeight/2;
      view.scrollTop = 0;
    },
    
    /*
     * Selects the next text channel and returns true, otherwise returns false if there are no more channels.
     */
    selectNextTextChannel: function(){
      var dms = document.querySelector("[class|='privateChannels']");
      
      if (dms){
        var nextChannel = DOM.queryReactClass("selected", dms).nextElementSibling;
        var nextLink = nextChannel && nextChannel.querySelector("a[href*='/@me/']");
        
        if (!nextChannel || !nextLink || !nextChannel.getAttribute("class").includes("channel-")){
          return false;
        }
        else{
          nextLink.click();
          return true;
        }
      }
      else{
        var isValidChannel = ele => ele.childElementCount > 0 && /wrapper([a-zA-Z]+?)Text/.test(ele.children[0].className);
        var allChannels = Array.prototype.filter.call(document.querySelector("[class|='channels']").querySelectorAll("[class|='containerDefault']"), isValidChannel);
        
        var nextChannel = null;
        
        for(var index = 0; index < allChannels.length-1; index++){
          if (allChannels[index].children[0].className.includes("wrapperSelectedText")){
            nextChannel = allChannels[index+1];
            break;
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
