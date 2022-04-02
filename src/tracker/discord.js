var DISCORD = (function(){
  var getMessageOuterElement = function(){
    return DOM.queryReactClass("messagesWrapper");
  };
  
  var getMessageScrollerElement = function(){
    return getMessageOuterElement().querySelector("[class*='scroller-']");
  };
  
  var getMessageElements = function() {
    return getMessageOuterElement().querySelectorAll("[class*='message-']");
  };
  
  var getReactProps = function(ele) {
    var keys = Object.keys(ele || {});
    var key = keys.find(key => key.startsWith("__reactInternalInstance"));
    
    if (key){
      return ele[key].memoizedProps;
    }
    
    key = keys.find(key => key.startsWith("__reactProps$"));
    return key ? ele[key] : null;
  };
  
  var getMessageElementProps = function(ele) {
    const props = getReactProps(ele);
    
    if (props.children && props.children.length >= 4) {
      const childProps = props.children[3].props;
      
      if ("message" in childProps && "channel" in childProps) {
        return childProps;
      }
    }
    
    return null;
  };
  
  var hasMoreMessages = function() {
    return document.querySelector("#messagesNavigationDescription + [class^=container]") === null;
  };
  
  var getMessages = function() {
    try {
      const messages = [];
      
      for (const ele of getMessageElements()) {
        const props = getMessageElementProps(ele);
        
        if (props != null) {
          messages.push(props.message);
        }
      }
      
      return messages;
    } catch (e) {
      console.error(e);
      return [];
    }
  };
  
  return {
    /**
     * Calls the provided function with a list of messages whenever the currently loaded messages change,
     * or with `false` if there are no more messages.
     */
    setupMessageCallback: function(callback) {
      let skipsLeft = 0;
      let waitForCleanup = false;
      let hasReachedStart = false;
      const previousMessages = new Set();
  
      const intervalId = window.setInterval(() => {
        if (skipsLeft > 0) {
          --skipsLeft;
          return;
        }
      
        const view = getMessageOuterElement();
      
        if (!view) {
          skipsLeft = 2;
          return;
        }
      
        const anyMessage = DOM.queryReactClass("message", getMessageOuterElement());
        const messageCount = anyMessage ? anyMessage.parentElement.children.length : 0;
      
        if (messageCount > 300) {
          if (waitForCleanup) {
            return;
          }
        
          skipsLeft = 3;
          waitForCleanup = true;
        
          window.setTimeout(() => {
            const view = getMessageScrollerElement();
            view.scrollTop = view.scrollHeight / 2;
          }, 1);
        }
        else {
          waitForCleanup = false;
        }
      
        const messages = getMessages();
        let hasChanged = false;
      
        for (const message of messages) {
          if (!previousMessages.has(message.id)) {
            hasChanged = true;
            break;
          }
        }
      
        if (!hasChanged) {
          if (!hasReachedStart && !hasMoreMessages()) {
            hasReachedStart = true;
            callback(false);
          }
          
          return;
        }
      
        previousMessages.clear();
        for (const message of messages) {
          previousMessages.add(message.id);
        }
        
        hasReachedStart = false;
        callback(messages);
      }, 200);
  
      window.DHT_ON_UNLOAD.push(() => window.clearInterval(intervalId));
    },
    
    /*
     * Returns internal React state object of an element.
     */
    getReactProps: function(ele){
      return getReactProps(ele);
    },
    
    /*
     * Returns an object containing the selected server name, selected channel name and ID, and the object type.
     * For types DM and GROUP, the server and channel names are identical.
     * For SERVER type, the channel has to be in view, otherwise Discord unloads it.
     */
    getSelectedChannel: function() {
      try {
        let obj;
        
        for (const ele of getMessageElements()) {
          const props = getMessageElementProps(ele);
          
          if (props != null) {
            obj = props.channel;
            break;
          }
        }
        
        if (!obj) {
          return null;
        }
        
        var dms = DOM.queryReactClass("privateChannels");
        
        if (dms){
          let name;
          
          for (const ele of dms.querySelectorAll("[class*='channel-'] [class*='selected-'] [class^='name-'] *, [class*='channel-'][class*='selected-'] [class^='name-'] *")) {
            const node = Array.prototype.find.call(ele.childNodes, node => node.nodeType === Node.TEXT_NODE);
            
            if (node) {
              name = node.nodeValue;
              break;
            }
          }
          
          if (!name) {
            return null;
          }
          
          let type;
          
          // https://discord.com/developers/docs/resources/channel#channel-object-channel-types
          switch (obj.type) {
            case 1: type = "DM"; break;
            case 3: type = "GROUP"; break;
            default: return null;
          }
          
          return {
            "server": name,
            "channel": name,
            "id": obj.id,
            "type": type,
            "extra": {}
          };
        }
        else if (obj.guild_id) {
          return {
            "server": document.querySelector("nav header h1[class*='name-']").innerText,
            "channel": obj.name,
            "id": obj.id,
            "type": "SERVER",
            "extra": {
              "position": obj.position,
              "topic": obj.topic,
              "nsfw": obj.nsfw
            }
          };
        }
        else {
          return null;
        }
      } catch(e) {
        console.error(e);
        return null;
      }
    },
    
    /*
     * Returns an array containing currently loaded messages.
     */
    getMessages: function(){
      return getMessages();
    },
    
    /*
     * Returns true if the message view is visible.
     */
    isInMessageView: () => !!getMessageOuterElement(),
    
    /*
     * Returns true if there are more messages available or if they're still loading.
     */
    hasMoreMessages: function(){
      return hasMoreMessages();
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
    selectNextTextChannel: function() {
      const dms = DOM.queryReactClass("privateChannels");
      
      if (dms) {
        const currentChannel = DOM.queryReactClass("selected", dms);
        const currentChannelContainer = currentChannel && currentChannel.closest("[class*='channel-']");
        const nextChannel = currentChannelContainer && currentChannelContainer.nextElementSibling;
        
        if (!nextChannel || !nextChannel.getAttribute("class").includes("channel-")) {
          return false;
        }
        
        const nextChannelLink = nextChannel.querySelector("a[href*='/@me/']");
        if (!nextChannelLink) {
          return false;
        }
        
        nextChannelLink.click();
        nextChannelLink.scrollIntoView(true);
        return true;
      }
      else {
        const channelListEle = document.getElementById("channels");
        if (!channelListEle) {
          return false;
        }
        
        function getLinkElement(channel) {
          return channel.querySelector("a[href^='/channels/'][role='link']");
        }
        
        const allTextChannels = Array.prototype.filter.call(channelListEle.querySelectorAll("[class*='containerDefault']"), ele => getLinkElement(ele) !== null);
        let nextChannel = null;
        
        for (let index = 0; index < allTextChannels.length - 1; index++) {
          if (allTextChannels[index].className.includes("selected-")) {
            nextChannel = allTextChannels[index + 1];
            break;
          }
        }
        
        if (nextChannel === null) {
          return false;
        }
        
        const nextChannelLink = getLinkElement(nextChannel);
        if (!nextChannelLink) {
          return false;
        }
        
        nextChannelLink.click();
        nextChannel.scrollIntoView(true);
        return true;
      }
    }
  };
})();
