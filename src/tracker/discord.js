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
          
          var name;
          
          for(let ele of channel.querySelectorAll("[class^='name']")){
            let node = Array.prototype.find.call(ele.childNodes, node => node.nodeType === Node.TEXT_NODE);
            
            if (node){
              name = node.nodeValue;
              break;
            }
          }
          
          if (!name){
            return null;
          }
          
          var icon = channel.querySelector("img[class*='avatar']");
          
          obj = {
            "server": name,
            "channel": name,
            "id": link,
            "type": (icon && icon.src.includes("/channel-icons/")) ? "GROUP" : "DM"
          };
        }
        else{
          channelListEle = document.querySelector("[class|='channels']");
          
          var channel = channelListEle.querySelector("[class*='modeSelected']").parentElement;
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
          let nested = obj.props.children;
          
          if (nested && nested.props && nested.props.messages){
            Array.prototype.push.apply(messages, nested.props.messages);
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
        var currentChannel = DOM.queryReactClass("selected", dms);
        var nextChannel = currentChannel && currentChannel.nextElementSibling;
        var nextLink = nextChannel && nextChannel.querySelector("a[href*='/@me/']");
        
        if (!nextChannel || !nextLink || !nextChannel.getAttribute("class").includes("channel-")){
          return false;
        }
        else{
          nextLink.click();
          nextChannel.scrollIntoView(true);
          return true;
        }
      }
      else{
        var isValidChannelClass = cls => cls.includes("wrapper-") && !cls.includes("clickable-");
        var isValidChannelType = ele => !!ele.querySelector('path[d="M5.88657 21C5.57547 21 5.3399 20.7189 5.39427 20.4126L6.00001 17H2.59511C2.28449 17 2.04905 16.7198 2.10259 16.4138L2.27759 15.4138C2.31946 15.1746 2.52722 15 2.77011 15H6.35001L7.41001 9H4.00511C3.69449 9 3.45905 8.71977 3.51259 8.41381L3.68759 7.41381C3.72946 7.17456 3.93722 7 4.18011 7H7.76001L8.39677 3.41262C8.43914 3.17391 8.64664 3 8.88907 3H9.87344C10.1845 3 10.4201 3.28107 10.3657 3.58738L9.76001 7H15.76L16.3968 3.41262C16.4391 3.17391 16.6466 3 16.8891 3H17.8734C18.1845 3 18.4201 3.28107 18.3657 3.58738L17.76 7H21.1649C21.4755 7 21.711 7.28023 21.6574 7.58619L21.4824 8.58619C21.4406 8.82544 21.2328 9 20.9899 9H17.41L16.35 15H19.7549C20.0655 15 20.301 15.2802 20.2474 15.5862L20.0724 16.5862C20.0306 16.8254 19.8228 17 19.5799 17H16L15.3632 20.5874C15.3209 20.8261 15.1134 21 14.8709 21H13.8866C13.5755 21 13.3399 20.7189 13.3943 20.4126L14 17H8.00001L7.36325 20.5874C7.32088 20.8261 7.11337 21 6.87094 21H5.88657ZM9.41045 9L8.35045 15H14.3504L15.4104 9H9.41045Z"]');
        var isValidChannel = ele => ele.childElementCount > 0 && isValidChannelClass(ele.children[0].className) && isValidChannelType(ele);
        
        var allChannels = Array.prototype.filter.call(document.querySelector("[class|='channels']").querySelectorAll("[class|='containerDefault']"), isValidChannel);
        var nextChannel = null;
        
        for(var index = 0; index < allChannels.length-1; index++){
          if (allChannels[index].children[0].className.includes("modeSelected")){
            nextChannel = allChannels[index+1];
            break;
          }
        }
        
        if (nextChannel === null){
          return false;
        }
        else{
          nextChannel.children[0].click();
          nextChannel.scrollIntoView(true);
          return true;
        }
      }
    }
  };
})();
