var DISCORD = (function(){
  var getMessageOuterElement = function(){
    return DOM.queryReactClass("messagesWrapper");
  };
  
  var getMessageScrollerElement = function(){
    return getMessageOuterElement().querySelector("[class*='scroller-']");
  };
  
  var observerTimer = 0, waitingForCleanup = 0;
  
  return {
    /*
     * Sets up a callback hook to trigger whenever the list of messages is updated. The callback is given a boolean value that is true if there are more messages to load.
     */
    setupMessageUpdateCallback: function(callback){
      var onTimerFinished = function(){
        let view = getMessageOuterElement();
        
        if (!view){
          restartTimer(500);
        }
        else{
          let anyMessage = getMessageOuterElement().querySelector("[class*='message-']");
          let messages = anyMessage ? anyMessage.parentElement.children.length : 0;
          
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
                let view = getMessageScrollerElement();
                view.scrollTop = view.scrollHeight/2;
              }, 1);
            }
            
            callback();
            restartTimer(200);
          }
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
      var keys = Object.keys(ele || {});
      var key = keys.find(key => key.startsWith("__reactInternalInstance"));
      
      if (key){
        return ele[key].memoizedProps;
      }
      
      key = keys.find(key => key.startsWith("__reactProps$"));
      return key ? ele[key] : null;
    },
    
    /*
     * Returns an object containing the selected server name, selected channel name and ID, and the object type.
     * For types DM and GROUP, the server and channel names are identical.
     * For SERVER type, the channel has to be in view, otherwise Discord unloads it.
     */
    getSelectedChannel: function(){
      try{
        var obj;
        var channelListEle = DOM.queryReactClass("privateChannels");
        
        if (channelListEle){
          var channel = DOM.queryReactClass("selected", channelListEle);
          
          if (!channel || !("href" in channel) || !channel.href.includes("/@me/")){
            return null;
          }
          
          var linkSplit = channel.href.split("/");
          var link = linkSplit[linkSplit.length-1];
          
          if (!(/^\d+$/.test(link))){
            return null;
          }
          
          var name;
          
          for(let ele of channel.querySelectorAll("[class^='name-'] *")){
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
          var iconParent = icon && icon.closest("foreignObject");
          var iconMask = iconParent && iconParent.getAttribute("mask");
          
          obj = {
            "server": name,
            "channel": name,
            "id": link,
            "type": (iconMask && iconMask.includes("#svg-mask-avatar-default")) ? "GROUP" : "DM",
            "extra": {}
          };
        }
        else{
          channelListEle = document.getElementById("channels");
          
          var channel = channelListEle.querySelector("[class*='modeSelected']").parentElement;
          var props = DISCORD.getReactProps(channel).children.props;
          
          if (!props){
            return null;
          }
          
          var channelObj = props.channel || props.children().props.channel;
          
          if (!channelObj){
            return null;
          }
          
          obj = {
            "server": document.querySelector("nav header > h1").innerText,
            "channel": channelObj.name,
            "id": channelObj.id,
            "type": "SERVER",
            "extra": {
              "position": channelObj.position,
              "topic": channelObj.topic,
              "nsfw": channelObj.nsfw
            }
          };
        }
        
        return obj.channel.length === 0 ? null : obj;
      }catch(e){
        console.error(e);
        return null;
      }
    },
    
    /*
     * Returns an array containing currently loaded messages.
     */
    getMessages: function(){
      try{
        var scroller = getMessageScrollerElement();
        var props = DISCORD.getReactProps(scroller);
        var wrappers;
        
        try{
          wrappers = props.children.props.children.props.children.props.children.find(ele => Array.isArray(ele));
        }catch(e){ // old version compatibility
          wrappers = props.children.find(ele => Array.isArray(ele));
        }
        
        var messages = [];
        
        for(let obj of wrappers){
          let nested = obj.props;
        
          if (nested && nested.message){
            messages.push(nested.message);
          }
        }
        
        return messages;
      }catch(e){
        console.error(e);
        return null;
      }
    },
    
    /*
     * Returns true if the message view is visible.
     */
    isInMessageView: () => !!getMessageOuterElement(),
    
    /*
     * Returns true if there are more messages available or if they're still loading.
     */
    hasMoreMessages: function(){
      return document.querySelector("#messagesNavigationDescription + [class^=container]") === null;
    },
    
    /*
     * Forces the message view to load older messages by scrolling all the way up.
     */
    loadOlderMessages: function(){
      let view = getMessageScrollerElement();
      
      if (view.scrollTop > 0){
        view.scrollTop = 0;
      }
    },
    
    /*
     * Selects the next text channel and returns true, otherwise returns false if there are no more channels.
     */
    selectNextTextChannel: function(){
      var dms = DOM.queryReactClass("privateChannels");
      
      if (dms){
        var currentChannel = DOM.queryReactClass("selected", dms);
        var nextChannel = currentChannel && currentChannel.nextElementSibling;
        
        if (!nextChannel || !nextChannel.getAttribute("class").includes("channel-") || !("href" in nextChannel) || !nextChannel.href.includes("/@me/")){
          return false;
        }
        else{
          nextChannel.click();
          nextChannel.scrollIntoView(true);
          return true;
        }
      }
      else{
        var channelIconNormal = "M5.88657 21C5.57547 21 5.3399 20.7189 5.39427 20.4126L6.00001 17H2.59511C2.28449 17 2.04905 16.7198 2.10259 16.4138L2.27759 15.4138C2.31946 15.1746 2.52722 15 2.77011 15H6.35001L7.41001 9H4.00511C3.69449 9 3.45905 8.71977 3.51259 8.41381L3.68759 7.41381C3.72946 7.17456 3.93722 7 4.18011 7H7.76001L8.39677 3.41262C8.43914 3.17391 8.64664 3 8.88907 3H9.87344C10.1845 3 10.4201 3.28107 10.3657 3.58738L9.76001 7H15.76L16.3968 3.41262C16.4391 3.17391 16.6466 3 16.8891 3H17.8734C18.1845 3 18.4201 3.28107 18.3657 3.58738L17.76 7H21.1649C21.4755 7 21.711 7.28023 21.6574 7.58619L21.4824 8.58619C21.4406 8.82544 21.2328 9 20.9899 9H17.41L16.35 15H19.7549C20.0655 15 20.301 15.2802 20.2474 15.5862L20.0724 16.5862C20.0306 16.8254 19.8228 17 19.5799 17H16L15.3632 20.5874C15.3209 20.8261 15.1134 21 14.8709 21H13.8866C13.5755 21 13.3399 20.7189 13.3943 20.4126L14 17H8.00001L7.36325 20.5874C7.32088 20.8261 7.11337 21 6.87094 21H5.88657ZM9.41045 9L8.35045 15H14.3504L15.4104 9H9.41045Z";
        var channelIconSpecial = "M14 8C14 7.44772 13.5523 7 13 7H9.76001L10.3657 3.58738C10.4201 3.28107 10.1845 3 9.87344 3H8.88907C8.64664 3 8.43914 3.17391 8.39677 3.41262L7.76001 7H4.18011C3.93722 7 3.72946 7.17456 3.68759 7.41381L3.51259 8.41381C3.45905 8.71977 3.69449 9 4.00511 9H7.41001L6.35001 15H2.77011C2.52722 15 2.31946 15.1746 2.27759 15.4138L2.10259 16.4138C2.04905 16.7198 2.28449 17 2.59511 17H6.00001L5.39427 20.4126C5.3399 20.7189 5.57547 21 5.88657 21H6.87094C7.11337 21 7.32088 20.8261 7.36325 20.5874L8.00001 17H14L13.3943 20.4126C13.3399 20.7189 13.5755 21 13.8866 21H14.8709C15.1134 21 15.3209 20.8261 15.3632 20.5874L16 17H19.5799C19.8228 17 20.0306 16.8254 20.0724 16.5862L20.2474 15.5862C20.301 15.2802 20.0655 15 19.7549 15H16.35L16.6758 13.1558C16.7823 12.5529 16.3186 12 15.7063 12C15.2286 12 14.8199 12.3429 14.7368 12.8133L14.3504 15H8.35045L9.41045 9H13C13.5523 9 14 8.55228 14 8Z";
        
        var isValidChannelClass = cls => cls.includes("wrapper-") && !cls.includes("clickable-");
        var isValidChannelType = ele => !!ele.querySelector('path[d="' + channelIconNormal + '"]') || !!ele.querySelector('path[d="' + channelIconSpecial + '"]');
        var isValidChannel = ele => ele.childElementCount > 0 && isValidChannelClass(ele.children[0].className) && isValidChannelType(ele);
        
        var channelListEle = document.querySelector("div[class*='sidebar'] > nav[class*='container'] > div[class*='scroller']");
        
        if (!channelListEle){
          return false;
        }
        
        var allChannels = Array.prototype.filter.call(channelListEle.querySelectorAll("[class*='containerDefault']"), isValidChannel);
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
