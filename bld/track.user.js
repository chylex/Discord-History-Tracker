// ==UserScript==
// @name         Discord History Tracker
// @version      v.29
// @license      MIT
// @namespace    https://chylex.com
// @homepageURL  https://dht.chylex.com/
// @supportURL   https://github.com/chylex/Discord-History-Tracker/issues
// @include      https://discord.com/*
// @run-at       document-idle
// @grant        none
// ==/UserScript==

const start = function(){

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

var DOM = (function(){
  var createElement = (tag, parent, id, html) => {
    var ele = document.createElement(tag);
    ele.id = id || "";
    ele.innerHTML = html || "";
    parent.appendChild(ele);
    return ele;
  };
  
  return {
    /*
     * Returns a child element by its ID. Parent defaults to the entire document.
     */
    id: (id, parent) => (parent || document).getElementById(id),
    
    /*
     * Returns the first child element containing the specified obfuscated class. Parent defaults to the entire document.
     */
    queryReactClass: (cls, parent) => (parent || document).querySelector(`[class*="${cls}-"]`),
    
    /*
     * Creates an element, adds it to the DOM, and returns it.
     */
    createElement: (tag, parent, id, html) => createElement(tag, parent, id, html),
    
    /*
     * Removes an element from the DOM.
     */
    removeElement: (ele) => ele.parentNode.removeChild(ele),
    
    /*
     * Creates a new style element with the specified CSS and returns it.
     */
    createStyle: (styles) => createElement("style", document.head, "", styles),
    
    /*
     * Convenience setTimeout function to save space after minification.
     */
    setTimer: (callback, timeout) => window.setTimeout(callback, timeout),
    
    /*
     * Convenience addEventListener function to save space after minification.
     */
    listen: (ele, event, callback) => ele.addEventListener(event, callback),
    
    /*
     * Utility function to save an object into a cookie.
     */
    saveToCookie: (name, obj, expiresInSeconds) => {
      var expires = new Date(Date.now()+1000*expiresInSeconds).toUTCString();
      document.cookie = name+"="+encodeURIComponent(JSON.stringify(obj))+";path=/;expires="+expires;
    },
    
    /*
     * Utility function to load an object from a cookie.
     */
    loadFromCookie: (name) => {
      var value = document.cookie.replace(new RegExp("(?:(?:^|.*;\\s*)"+name+"\\s*\\=\\s*([^;]*).*$)|^.*$"), "$1");
      return value.length ? JSON.parse(decodeURIComponent(value)) : null;
    },
    
    /*
     * Triggers a UTF-8 text file download.
     */
    downloadTextFile: (fileName, fileContents) => {
      var blob = new Blob([fileContents], { "type": "octet/stream" });
      
      if ("msSaveBlob" in window.navigator){
        return window.navigator.msSaveBlob(blob, fileName);
      }
      
      var url = window.URL.createObjectURL(blob);
      
      var ele = createElement("a", document.body);
      ele.href = url;
      ele.download = fileName;
      ele.style.display = "none";
      
      ele.click();
      
      document.body.removeChild(ele);
      window.URL.revokeObjectURL(url);
    }
  };
})();

var GUI = (function(){
  var controller;
  var settings;
  
  var updateButtonState = () => {
    if (STATE.isTracking()){
      controller.ui.btnUpload.disabled = true;
      controller.ui.btnSettings.disabled = true;
      controller.ui.btnReset.disabled = true;
    }
    else{
      controller.ui.btnUpload.disabled = false;
      controller.ui.btnSettings.disabled = false;
      controller.ui.btnDownload.disabled = controller.ui.btnReset.disabled = !STATE.hasSavedData();
    }
  };
  
  var stateChangedEvent = (type, detail) => {
    if (controller){
      var force = type === "gui" && detail === "controller";
      
      if (type === "data" || force){
        updateButtonState();
      }
      
      if (type === "tracking" || force){
        updateButtonState();
        controller.ui.btnToggleTracking.innerHTML = STATE.isTracking() ? "Pause Tracking" : "Start Tracking";
      }
      
      if (type === "data" || force){
        var messageCount = 0;
        var channelCount = 0;
        
        if (STATE.hasSavedData()){
          messageCount = STATE.getSavefile().countMessages();
          channelCount = STATE.getSavefile().countChannels();
        }
        
        controller.ui.textStatus.innerHTML = [
          messageCount, " message", (messageCount === 1 ? "" : "s"),
          " from ",
          channelCount, " channel", (channelCount === 1 ? "" : "s")
        ].join("");
      }
    }
    
    if (settings){
      var force = type === "gui" && detail === "settings";
      
      if (force){
        settings.ui.cbAutoscroll.checked = SETTINGS.autoscroll;
        settings.ui.optsAfterFirstMsg[SETTINGS.afterFirstMsg].checked = true;
        settings.ui.optsAfterSavedMsg[SETTINGS.afterSavedMsg].checked = true;
      }
      
      if (type === "setting" || force){
        var autoscrollRev = !SETTINGS.autoscroll;
        
        // discord polyfills Object.values
        Object.values(settings.ui.optsAfterFirstMsg).forEach(ele => ele.disabled = autoscrollRev);
        Object.values(settings.ui.optsAfterSavedMsg).forEach(ele => ele.disabled = autoscrollRev);
      }
    }
  };
  
  var registeredEvent = false;
  
  var setupStateChanged = function(detail){
    if (!registeredEvent){
      STATE.onStateChanged(stateChangedEvent);
      SETTINGS.onSettingsChanged(stateChangedEvent);
      registeredEvent = true;
    }
    
    stateChangedEvent("gui", detail);
  };
  
  var root = {
    showController: function(){
      controller = {};
      
      // styles
      
      controller.styles = DOM.createStyle(`
#app-mount > div[class*="app-"] { margin-bottom: 48px !important; }
#dht-ctrl { position: absolute; bottom: 0; width: 100%; height: 48px; background-color: #FFF; }
#dht-ctrl button { height: 32px; margin: 8px 0 8px 8px; font-size: 16px; padding: 0 12px; background-color: #7289DA; color: #FFF; text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.75); }
#dht-ctrl button:disabled { background-color: #7A7A7A; cursor: default; }
#dht-ctrl-close { margin: 8px 8px 8px 0 !important; float: right; }
#dht-ctrl p { display: inline-block; margin: 14px 12px; }
#dht-ctrl input { display: none; }`);
      
      // main
      
      var btn = (id, title) => "<button id='dht-ctrl-"+id+"'>"+title+"</button>";
      
      controller.ele = DOM.createElement("div", document.body, "dht-ctrl", `
${btn("upload", "Upload &amp; Combine")}
${btn("settings", "Settings")}
${btn("track", "")}
${btn("download", "Download")}
${btn("reset", "Reset")}
<p id='dht-ctrl-status'></p>
<input id='dht-ctrl-upload-input' type='file' multiple>
${btn("close", "X")}`);
      
      // elements
      
      controller.ui = {
        btnUpload: DOM.id("dht-ctrl-upload"),
        btnSettings: DOM.id("dht-ctrl-settings"),
        btnToggleTracking: DOM.id("dht-ctrl-track"),
        btnDownload: DOM.id("dht-ctrl-download"),
        btnReset: DOM.id("dht-ctrl-reset"),
        btnClose: DOM.id("dht-ctrl-close"),
        textStatus: DOM.id("dht-ctrl-status"),
        inputUpload: DOM.id("dht-ctrl-upload-input")
      };
      
      // events
      
      DOM.listen(controller.ui.btnUpload, "click", () => {
        controller.ui.inputUpload.click();
      });
      
      DOM.listen(controller.ui.btnSettings, "click", () => {
        root.showSettings();
      });
      
      DOM.listen(controller.ui.btnToggleTracking, "click", () => {
        STATE.setIsTracking(!STATE.isTracking());
      });
      
      DOM.listen(controller.ui.btnDownload, "click", () => {
        STATE.downloadSavefile();
      });
      
      DOM.listen(controller.ui.btnReset, "click", () => {
        STATE.resetState();
      });
      
      DOM.listen(controller.ui.btnClose, "click", () => {
        root.hideController();
        window.DHT_ON_UNLOAD.forEach(f => f());
        window.DHT_LOADED = false;
      });
      
      DOM.listen(controller.ui.inputUpload, "change", () => {
        Array.prototype.forEach.call(controller.ui.inputUpload.files, file => {
          var reader = new FileReader();
          
          reader.onload = function(){
            var obj = {};

            try{
              obj = JSON.parse(reader.result);
            }catch(e){
              alert("Could not parse '"+file.name+"', see console for details.");
              console.error(e);
              return;
            }
            
            if (SAVEFILE.isValid(obj)){
              STATE.uploadSavefile(file.name, new SAVEFILE(obj));
            }
            else{
              alert("File '"+file.name+"' has an invalid format.");
            }
          };
          
          reader.readAsText(file, "UTF-8");
        });

        controller.ui.inputUpload.value = null;
      });
      
      setupStateChanged("controller");
    },
    
    hideController: function(){
      if (controller){
        DOM.removeElement(controller.ele);
        DOM.removeElement(controller.styles);
        controller = null;
      }
    },
    
    showSettings: function(){
      settings = {};
      
      // styles
      
      settings.styles = DOM.createStyle(`
#dht-cfg-overlay { position: absolute; left: 0; top: 0; width: 100%; height: 100%; background-color: #000; opacity: 0.5; display: block; z-index: 1000; }
#dht-cfg { position: absolute; left: 50%; top: 50%; width: 800px; height: 262px; margin-left: -400px; margin-top: -131px; padding: 8px; background-color: #fff; z-index: 1001; }
#dht-cfg-note { margin-top: 22px; }
#dht-cfg sub { color: #666; font-size: 13px; }`);
      
      // overlay
      
      settings.overlay = DOM.createElement("div", document.body, "dht-cfg-overlay");
      
      DOM.listen(settings.overlay, "click", () => {
        root.hideSettings();
      });
      
      // main
      
      var radio = (type, id, label) => "<label><input id='dht-cfg-"+type+"-"+id+"' name='dht-"+type+"' type='radio'> "+label+"</label><br>";
      
      settings.ele = DOM.createElement("div", document.body, "dht-cfg", `
<label><input id='dht-cfg-autoscroll' type='checkbox'> Autoscroll</label><br>
<br>
<label>After reaching the first message in channel...</label><br>
${radio("afm", "nothing", "Do Nothing")}
${radio("afm", "pause", "Pause Tracking")}
${radio("afm", "switch", "Switch to Next Channel")}
<br>
<label>After reaching a previously saved message...</label><br>
${radio("asm", "nothing", "Do Nothing")}
${radio("asm", "pause", "Pause Tracking")}
${radio("asm", "switch", "Switch to Next Channel")}
<p id='dht-cfg-note'>
It is recommended to disable link and image previews to avoid putting unnecessary strain on your browser.<br><br>
<sub>v.29, released 20 Dec 2020</sub>
</p>`);
      
      // elements
      
      settings.ui = {
        cbAutoscroll: DOM.id("dht-cfg-autoscroll"),
        optsAfterFirstMsg: {},
        optsAfterSavedMsg: {}
      };
      
      settings.ui.optsAfterFirstMsg[CONSTANTS.AUTOSCROLL_ACTION_NOTHING] = DOM.id("dht-cfg-afm-nothing");
      settings.ui.optsAfterFirstMsg[CONSTANTS.AUTOSCROLL_ACTION_PAUSE] = DOM.id("dht-cfg-afm-pause");
      settings.ui.optsAfterFirstMsg[CONSTANTS.AUTOSCROLL_ACTION_SWITCH] = DOM.id("dht-cfg-afm-switch");
      
      settings.ui.optsAfterSavedMsg[CONSTANTS.AUTOSCROLL_ACTION_NOTHING] = DOM.id("dht-cfg-asm-nothing");
      settings.ui.optsAfterSavedMsg[CONSTANTS.AUTOSCROLL_ACTION_PAUSE] = DOM.id("dht-cfg-asm-pause");
      settings.ui.optsAfterSavedMsg[CONSTANTS.AUTOSCROLL_ACTION_SWITCH] = DOM.id("dht-cfg-asm-switch");
      
      // events
      
      settings.ui.cbAutoscroll.addEventListener("change", () => {
        SETTINGS.autoscroll = settings.ui.cbAutoscroll.checked;
      });
      
      Object.keys(settings.ui.optsAfterFirstMsg).forEach(key => {
        DOM.listen(settings.ui.optsAfterFirstMsg[key], "click", () => {
          SETTINGS.afterFirstMsg = key;
        });
      });
      
      Object.keys(settings.ui.optsAfterSavedMsg).forEach(key => {
        DOM.listen(settings.ui.optsAfterSavedMsg[key], "click", () => {
          SETTINGS.afterSavedMsg = key;
        });
      });
      
      setupStateChanged("settings");
    },
    
    hideSettings: function(){
      if (settings){
        DOM.removeElement(settings.overlay);
        DOM.removeElement(settings.ele);
        DOM.removeElement(settings.styles);
        settings = null;
      }
    }
  };
  
  return root;
})();

/*
 * SAVEFILE STRUCTURE
 * ==================
 *
 * {
 *   meta: {
 *     users: {
 *       <discord user id>: {
 *         name: <user name>,
 *         avatar: <user icon>,
 *         tag: <user discriminator> // only present if not a bot
 *       }, ...
 *     },
 *
 *     // the user index is an array of discord user ids,
 *     // these indexes are used in the message objects to save space
 *     userindex: [
 *       <discord user id>, ...
 *     ],
 *
 *     servers: [
 *       {
 *         name: <server name>,
 *         type: <"SERVER"|"GROUP"|DM">
 *       }, ...
 *     ],
 *
 *     channels: {
 *       <discord channel id>: {
 *         server: <server index in the meta.servers array>,
 *         name: <channel name>,
 *         position: <order in channel list>, // only present if server type == SERVER
 *         topic: <channel topic>,            // only present if server type == SERVER
 *         nsfw: <channel NSFW status>        // only present if server type == SERVER
 *       }, ...
 *     }
 *   },
 *
 *   data: {
 *     <discord channel id>: {
 *       <discord message id>: {
 *         u: <user index of the sender>,
 *         t: <message timestamp>,
 *         m: <message content>, // only present if not empty
 *         f: <message flags>,   // only present if edited in which case it equals 1, deprecated (use 'te' instead),
 *         te: <edit timestamp>, // only present if edited,
 *         e: [ // omit for no embeds
 *           {
 *             url: <embed url>,
 *             type: <embed type>,
 *             t: <rich embed title>,      // only present if type == rich, and if not empty
 *             d: <rich embed description> // only present if type == rich, and if the embed has a simple description text
 *           }, ...
 *         ],
 *         a: [ // omit for no attachments
 *           {
 *             url: <attachment url>
 *           }, ...
 *         ]
 *       }, ...
 *     }, ...
 *   }
 * }
 *
 *
 * TEMPORARY OBJECT STRUCTURE
 * ==========================
 *
 * {
 *   userlookup: {
 *     <discord user id>: <user index in the meta.userindex array>
 *   },
 *   channelkeys: Set<channel id>,
 *   messagekeys: Set<message id>,
 *   freshmsgs: Set<message id> // only messages which were newly added to the savefile in the current session
 * }
 */

class SAVEFILE{
  constructor(parsedObj){
    var me = this;
    
    if (!SAVEFILE.isValid(parsedObj)){
      parsedObj = {
        meta: {},
        data: {}
      };
    }
    
    me.meta = parsedObj.meta;
    me.data = parsedObj.data;
    
    me.meta.users = me.meta.users || {};
    me.meta.userindex = me.meta.userindex || [];
    me.meta.servers = me.meta.servers || [];
    me.meta.channels = me.meta.channels || {};
    
    me.tmp = {
      userlookup: {},
      channelkeys: new Set(),
      messagekeys: new Set(),
      freshmsgs: new Set()
    }
  }
  
  static isValid(parsedObj){
    return parsedObj && typeof parsedObj.meta === "object" && typeof parsedObj.data === "object";
  }
  
  findOrRegisterUser(userId, userName, userDiscriminator, userAvatar){
    var wasPresent = userId in this.meta.users;
    var userObj = wasPresent ? this.meta.users[userId] : {};
    
    userObj.name = userName;
    
    if (userDiscriminator){
      userObj.tag = userDiscriminator;
    }
    
    if (userAvatar){
      userObj.avatar = userAvatar;
    }
    
    if (!wasPresent){
      this.meta.users[userId] = userObj;
      this.meta.userindex.push(userId);
      return this.tmp.userlookup[userId] = this.meta.userindex.length-1;
    }
    else if (!(userId in this.tmp.userlookup)){
      return this.tmp.userlookup[userId] = this.meta.userindex.findIndex(id => id == userId);
    }
    else{
      return this.tmp.userlookup[userId];
    }
  }
  
  findOrRegisterServer(serverName, serverType){
    var index = this.meta.servers.findIndex(server => server.name === serverName && server.type === serverType);
    
    if (index === -1){
      this.meta.servers.push({
        "name": serverName,
        "type": serverType
      });
      
      return this.meta.servers.length-1;
    }
    else{
      return index;
    }
  }
  
  tryRegisterChannel(serverIndex, channelId, channelName, extraInfo){
    if (!this.meta.servers[serverIndex]){
      return undefined;
    }
    
    var wasPresent = channelId in this.meta.channels;
    var channelObj = wasPresent ? this.meta.channels[channelId] : { "server": serverIndex };
    
    channelObj.name = channelName;
    
    if (extraInfo.position){
      channelObj.position = extraInfo.position;
    }
    
    if (extraInfo.topic){
      channelObj.topic = extraInfo.topic;
    }
    
    if (extraInfo.nsfw){
      channelObj.nsfw = extraInfo.nsfw;
    }
    
    if (wasPresent){
      return false;
    }
    else{
      this.meta.channels[channelId] = channelObj;
      this.tmp.channelkeys.add(channelId);
      return true;
    }
  }
  
  addMessage(channelId, messageId, messageObject){
    var container = this.data[channelId] || (this.data[channelId] = {});
    var wasPresent = messageId in container;
    
    container[messageId] = messageObject;
    this.tmp.messagekeys.add(messageId);
    return !wasPresent;
  }
  
  convertToMessageObject(discordMessage){
    var author = discordMessage.author;
    
    var obj = {
      u: this.findOrRegisterUser(author.id, author.username, author.bot ? null : author.discriminator, author.avatar),
      t: discordMessage.timestamp.toDate().getTime()
    };
    
    if (discordMessage.content.length > 0){
      obj.m = discordMessage.content;
    }
    
    if (discordMessage.editedTimestamp !== null){
      obj.te = discordMessage.editedTimestamp.toDate().getTime();
    }
    
    if (discordMessage.embeds.length > 0){
      obj.e = discordMessage.embeds.map(embed => {
        let conv = {
          url: embed.url,
          type: embed.type
        };
        
        if (embed.type === "rich"){
          if (Array.isArray(embed.title) && embed.title.length === 1 && typeof embed.title[0] === "string"){
            conv.t = embed.title[0];
            
            if (Array.isArray(embed.description) && embed.description.length === 1 && typeof embed.description[0] === "string"){
              conv.d = embed.description[0];
            }
          }
        }
        
        return conv;
      });
    }
    
    if (discordMessage.attachments.length > 0){
      obj.a = discordMessage.attachments.map(attachment => ({
        url: attachment.url
      }));
    }
    
    return obj;
  }
  
  isMessageFresh(id){
    return this.tmp.freshmsgs.has(id);
  }
  
  addMessagesFromDiscord(channelId, discordMessageArray){
    var hasNewMessages = false;
    
    for(var discordMessage of discordMessageArray){
      var type = discordMessage.type;
      
      // https://discord.com/developers/docs/resources/channel#message-object-message-reference-structure
      if ((type === 0 || type === 19) && discordMessage.state === "SENT" && this.addMessage(channelId, discordMessage.id, this.convertToMessageObject(discordMessage))){
        this.tmp.freshmsgs.add(discordMessage.id);
        hasNewMessages = true;
      }
    }
    
    return hasNewMessages;
  }
  
  countChannels(){
    return this.tmp.channelkeys.size;
  }
  
  countMessages(){
    return this.tmp.messagekeys.size;
  }
  
  combineWith(obj){
    var userMap = {};
    var shownError = false;
    
    for(var userId in obj.meta.users){
      var oldUser = obj.meta.users[userId];
      userMap[obj.meta.userindex.findIndex(id => id == userId)] = this.findOrRegisterUser(userId, oldUser.name, oldUser.tag, oldUser.avatar);
    }
    
    for(var channelId in obj.meta.channels){
      var oldServer = obj.meta.servers[obj.meta.channels[channelId].server];
      var oldChannel = obj.meta.channels[channelId];
      this.tryRegisterChannel(this.findOrRegisterServer(oldServer.name, oldServer.type), channelId, oldChannel.name, oldChannel /* filtered later */);
    }
    
    for(var channelId in obj.data){
      var oldChannel = obj.data[channelId];
      
      for(var messageId in oldChannel){
        var oldMessage = oldChannel[messageId];
        var oldUser = oldMessage.u;
        
        if (oldUser in userMap){
          oldMessage.u = userMap[oldUser];
          this.addMessage(channelId, messageId, oldMessage);
        }
        else{
          if (!shownError){
            shownError = true;
            alert("The uploaded archive appears to be corrupted, some messages will be skipped. See console for details.");
            
            console.error("User list:", obj.meta.users);
            console.error("User index:", obj.meta.userindex);
            console.error("Generated mapping:", userMap);
            console.error("Missing user for the following messages:");
          }
          
          console.error(oldMessage);
        }
      }
    }
  }
  
  toJson(){
    return JSON.stringify({
      "meta": this.meta,
      "data": this.data
    });
  }
}

var CONSTANTS = {
  AUTOSCROLL_ACTION_NOTHING: "optNothing",
  AUTOSCROLL_ACTION_PAUSE: "optPause",
  AUTOSCROLL_ACTION_SWITCH: "optSwitch"
};

var IS_FIRST_RUN = false;

var SETTINGS = (function(){
  var root = {};
  var settingsChangedEvents = [];
  
  var saveSettings = function(){
    DOM.saveToCookie("DHT_SETTINGS", root, 60*60*24*365*5);
  };
  
  var triggerSettingsChanged = function(changeType, changeDetail){
    for(var callback of settingsChangedEvents){
      callback(changeType, changeDetail);
    }
    
    saveSettings();
  };
  
  var defineTriggeringProperty = function(obj, property, value){
    var name = "_"+property;
    
    Object.defineProperty(obj, property, {
      get: (() => obj[name]),
      set: (value => {
        obj[name] = value;
        triggerSettingsChanged("setting", property);
      })
    });
    
    obj[name] = value;
  };
  
  var loaded = DOM.loadFromCookie("DHT_SETTINGS");
  
  if (!loaded){
    loaded = {
      "_autoscroll": true,
      "_afterFirstMsg": CONSTANTS.AUTOSCROLL_ACTION_PAUSE,
      "_afterSavedMsg": CONSTANTS.AUTOSCROLL_ACTION_PAUSE
    };
    
    IS_FIRST_RUN = true;
  }
  
  defineTriggeringProperty(root, "autoscroll", loaded._autoscroll);
  defineTriggeringProperty(root, "afterFirstMsg", loaded._afterFirstMsg);
  defineTriggeringProperty(root, "afterSavedMsg", loaded._afterSavedMsg);
  
  root.onSettingsChanged = function(callback){
    settingsChangedEvents.push(callback);
  };
  
  if (IS_FIRST_RUN){
    saveSettings();
  }
  
  return root;
})();

var STATE = (function(){
  var stateChangedEvents = [];
  
  var triggerStateChanged = function(changeType, changeDetail){
    for(var callback of stateChangedEvents){
      callback(changeType, changeDetail);
    }
  };
  
  /*
   * Internal class constructor.
   */
  class CLS{
    constructor(){
      this.resetState();
    };
    
    /*
     * Resets the state to default values.
     */
    resetState(){
      this._savefile = null;
      this._isTracking = false;
      this._lastFileName = null;
      triggerStateChanged("data", "reset");
    }
    
    /*
     * Returns the savefile object, creates a new one if needed.
     */
    getSavefile(){
      if (!this._savefile){
        this._savefile = new SAVEFILE();
      }
      
      return this._savefile;
    }
    
    /*
     * Returns true if the database file contains any data.
     */
    hasSavedData(){
      return this._savefile != null;
    }
    
    /*
     * Returns true if currently tracking message.
     */
    isTracking(){
      return this._isTracking;
    }
    
    /*
     * Sets the tracking state.
     */
    setIsTracking(state){
      this._isTracking = state;
      triggerStateChanged("tracking", state);
    }
    
    /*
     * Combines current savefile with the provided one.
     */
    uploadSavefile(fileName, fileObject){
      this._lastFileName = fileName;
      this.getSavefile().combineWith(fileObject);
      triggerStateChanged("data", "upload");
    }
    
    /*
     * Triggers a savefile download, if available.
     */
    downloadSavefile(){
      if (this.hasSavedData()){
        DOM.downloadTextFile(this._lastFileName || "dht.txt", this._savefile.toJson());
      }
    }
    
    /*
     * Registers a Discord server and channel.
     */
    addDiscordChannel(serverName, serverType, channelId, channelName, extraInfo){
      var serverIndex = this.getSavefile().findOrRegisterServer(serverName, serverType);
      
      if (this.getSavefile().tryRegisterChannel(serverIndex, channelId, channelName, extraInfo) === true){
        triggerStateChanged("data", "channel");
      }
    }
    
    /*
     * Adds all messages from the array to the specified channel. Returns true if the savefile was updated.
     */
    addDiscordMessages(channelId, discordMessageArray){
      if (this.getSavefile().addMessagesFromDiscord(channelId, discordMessageArray)){
        triggerStateChanged("data", "messages");
        return true;
      }
      else{
        return false;
      }
    }
    
    /*
     * Returns true if the message was added during this session.
     */
    isMessageFresh(id){
      return this.getSavefile().isMessageFresh(id);
    }
    
    /*
     * Adds a listener that is called whenever the state changes. The callback is a function that takes subject (generic type) and detail (specific type or data).
     */
    onStateChanged(callback){
      stateChangedEvents.push(callback);
    }
  }
  
  return new CLS();
})();

const url = window.location.href;

if (!url.includes("discord.com/") && !url.includes("discordapp.com/") && !confirm("Could not detect Discord in the URL, do you want to run the script anyway?")){
  return;
}

if (window.DHT_LOADED){
  alert("Discord History Tracker is already loaded.");
  return;
}

window.DHT_LOADED = true;
window.DHT_ON_UNLOAD = [];

// Execution

let ignoreMessageCallback = new Set();
let frozenMessageLoadingTimer = null;

let stopTrackingDelayed = function(callback){
  ignoreMessageCallback.add("stopping");
  
  DOM.setTimer(() => {
    STATE.setIsTracking(false);
    ignoreMessageCallback.delete("stopping");
    
    if (callback){
      callback();
    }
  }, 200); // give the user visual feedback after clicking the button before switching off
};

DISCORD.setupMessageUpdateCallback(() => {
  if (STATE.isTracking() && ignoreMessageCallback.size === 0){
    let info = DISCORD.getSelectedChannel();
    
    if (!info){
      stopTrackingDelayed();
      return;
    }
    
    STATE.addDiscordChannel(info.server, info.type, info.id, info.channel, info.extra);
    
    let messages = DISCORD.getMessages();
    
    if (messages == null){
      stopTrackingDelayed();
      return;
    }
    else if (!messages.length){
      DISCORD.loadOlderMessages();
      return;
    }
    
    let hasUpdatedFile = STATE.addDiscordMessages(info.id, messages);
    
    if (SETTINGS.autoscroll){
      let action = null;
      
      if (!hasUpdatedFile && !STATE.isMessageFresh(messages[0].id)){
        action = SETTINGS.afterSavedMsg;
      }
      else if (!DISCORD.hasMoreMessages()){
        action = SETTINGS.afterFirstMsg;
      }
      
      if (action === null){
        if (hasUpdatedFile){
          DISCORD.loadOlderMessages();
          window.clearTimeout(frozenMessageLoadingTimer);
          frozenMessageLoadingTimer = null;
        }
        else{
          frozenMessageLoadingTimer = window.setTimeout(DISCORD.loadOlderMessages, 2500);
        }
      }
      else{
        ignoreMessageCallback.add("stalling");
        
        DOM.setTimer(() => {
          ignoreMessageCallback.delete("stalling");
          
          let updatedInfo = DISCORD.getSelectedChannel();
          
          if (updatedInfo && updatedInfo.id === info.id){
            let lastMessages = DISCORD.getMessages(); // sometimes needed to catch the last few messages before switching
            
            if (lastMessages != null){
              STATE.addDiscordMessages(info.id, lastMessages);
            }
          }
          
          if ((action === CONSTANTS.AUTOSCROLL_ACTION_SWITCH && !DISCORD.selectNextTextChannel()) || action === CONSTANTS.AUTOSCROLL_ACTION_PAUSE){
            STATE.setIsTracking(false);
          }
        }, 250);
      }
    }
  }
});

STATE.onStateChanged((type, enabled) => {
  if (type === "tracking" && enabled){
    let info = DISCORD.getSelectedChannel();
    
    if (info){
      let messages = DISCORD.getMessages();
      
      if (messages != null){
        STATE.addDiscordChannel(info.server, info.type, info.id, info.channel, info.extra);
        STATE.addDiscordMessages(info.id, messages);
      }
      else{
        stopTrackingDelayed(() => alert("Cannot see any messages."));
        return;
      }
    }
    else{
      stopTrackingDelayed(() => alert("The selected channel is not visible in the channel list."));
      return;
    }
    
    if (SETTINGS.autoscroll && DISCORD.isInMessageView()){
      if (DISCORD.hasMoreMessages()){
        DISCORD.loadOlderMessages();
      }
      else{
        let action = SETTINGS.afterFirstMsg;
        
        if ((action === CONSTANTS.AUTOSCROLL_ACTION_SWITCH && !DISCORD.selectNextTextChannel()) || action === CONSTANTS.AUTOSCROLL_ACTION_PAUSE){
          stopTrackingDelayed();
        }
      }
    }
  }
});

GUI.showController();

if (IS_FIRST_RUN){
  GUI.showSettings();
}


};

const css = document.createElement("style");

css.innerText = `
#dht-userscript-trigger { cursor: pointer; margin-top: 5px }
#dht-userscript-trigger svg { opacity: 0.6 }
#dht-userscript-trigger:hover svg { opacity: 1 }
`;

document.head.appendChild(css);

window.setInterval(function(){
  if (document.getElementById("dht-userscript-trigger")){
    return;
  }
  
  const help = document.querySelector("section[class^='title'] a[href*='support.discord.com']");
  
  if (help){
    help.insertAdjacentHTML("afterend", `
<span id="dht-userscript-trigger">
  <span style="margin: 0 4px" role="button">
    <svg width="28" height="16" viewBox="0 0 11 6" fill="#fff">
      <path d="M3.133,2.848c0,0.355 -0.044,0.668 -0.132,0.937c-0.088,0.27 -0.208,0.495 -0.36,0.677c-0.153,0.181 -0.333,0.319 -0.541,0.412c-0.207,0.092 -0.431,0.139 -0.672,0.139l-1.413,0l0,-4.266l1.265,0c0.27,0 0.519,0.042 0.746,0.124c0.227,0.083 0.423,0.21 0.586,0.382c0.164,0.171 0.291,0.389 0.383,0.654c0.092,0.264 0.138,0.578 0.138,0.941Zm-0.739,0c0,-0.248 -0.028,-0.461 -0.083,-0.639c-0.056,-0.177 -0.133,-0.323 -0.232,-0.437c-0.099,-0.114 -0.217,-0.198 -0.355,-0.253c-0.139,-0.054 -0.292,-0.082 -0.459,-0.082l-0.518,0l0,2.886l0.621,0c0.147,0 0.283,-0.032 0.409,-0.094c0.125,-0.063 0.233,-0.156 0.325,-0.28c0.092,-0.124 0.163,-0.278 0.215,-0.462c0.051,-0.184 0.077,-0.397 0.077,-0.639Z"></path>
      <path d="M5.939,5.013l0,-1.829l-1.523,0l0,1.829l-0.732,0l0,-4.266l0.732,0l0,1.699l1.523,0l0,-1.699l0.733,0l0,4.266l-0.733,0Z"></path>
      <path d="M8.933,1.437l0,3.576l-0.732,0l0,-3.576l-1.13,0l0,-0.69l2.994,0l0,0.69l-1.132,0Z"></path>
    </svg>
  </span>
</span>`);
    
    document.getElementById("dht-userscript-trigger").addEventListener("click", start);
  }
}, 200);
