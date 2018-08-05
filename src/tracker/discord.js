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

        var channelListEle = DOM.fcls("private-channels");
        var channel;

        if (channelListEle){
          channel = DOM.fcls("selected", channelListEle);

          if (!channel.classList.contains("private")){
            return null;
          }
          
          var linkSplit = DOM.ftag("a", channel).href.split("/");
          var name = [].find.call(DOM.fcls("channel-name", channel).childNodes, node => node.nodeType === Node.TEXT_NODE).nodeValue;
          
          if (!name){
            return null;
          }

          obj = {
            "server": name,
            "channel": name,
            "id": linkSplit[linkSplit.length-1],
            "type": !!DOM.fcls("status", channel) ? "DM" : "GROUP"
          };
        }
        else{
          channelListEle = document.querySelector("[class|='channels']");
          channel = channelListEle.querySelector("[class|='wrapperSelectedText']").parentElement;
          
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
      var dms = DOM.fcls("private-channels");
      
      if (dms){
        var nextChannel = DOM.fcls("selected", dms).nextElementSibling;
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
        var allChannels = document.querySelector("[class|='channels']").querySelectorAll("[class|='containerDefault']");
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
