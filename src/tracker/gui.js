var GUI = (function(){
  var controller;
  var settings;
  
  var stateChangedEvent = (type, detail) => {
    if (controller){
      var force = type === "gui" && detail === "controller";
      
      if (type === "data" || force){
        controller.ui.btnDownload.disabled = controller.ui.btnReset.disabled = !STATE.hasSavedData();
      }
      
      if (type === "tracking" || force){
        controller.ui.btnUpload.disabled = controller.ui.btnSettings.disabled = STATE.isTracking();
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
        settings.ui.cbAutoscroll.checked = STATE.settings.autoscroll;
        settings.ui.optsAfterFirstMsg[STATE.settings.afterFirstMsg].checked = true;
        settings.ui.optsAfterSavedMsg[STATE.settings.afterSavedMsg].checked = true;
      }
      
      if (type === "setting" || force){
        var autoscrollRev = !STATE.settings.autoscroll;
        
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
      registeredEvent = true;
    }
    
    stateChangedEvent("gui", detail);
  };
  
  var root = {
    showController: function(){
      controller = {};
      
      // styles
      
      controller.styles = DOM.createStyle([
        ".app, .connecting { bottom: 48px !important; }",
        "#dht-ctrl { position: absolute; bottom: 0; width: 100%; height: 48px; background-color: #fff; }",
        "#dht-ctrl button { height: 32px; margin: 8px 0 8px 8px; font-size: 18px; padding: 0 12px; background-color: #adf; }",
        "#dht-ctrl button:disabled { background-color: #d0d0d0; cursor: default; }",
        "#dht-ctrl-close { margin: 8px 8px 8px 0 !important; float: right; }",
        "#dht-ctrl p { display: inline-block; margin: 14px 12px; }",
        "#dht-ctrl input { display: none; }"
      ]);
      
      // main
      
      controller.ele = DOM.createElement("div", document.body);
      controller.ele.id = "dht-ctrl";
      
      controller.ele.innerHTML = [
        "<button id='dht-ctrl-upload'>Upload &amp; Combine</button>",
        "<button id='dht-ctrl-settings'>Settings</button>",
        "<button id='dht-ctrl-track'></button>",
        "<button id='dht-ctrl-download'>Download</button>",
        "<button id='dht-ctrl-reset'>Reset</button>",
        "<p id='dht-ctrl-status'></p>",
        "<input id='dht-ctrl-upload-input' type='file' multiple>",
        "<button id='dht-ctrl-close'>X</button>"
      ].join("");
      
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
      
      controller.ui.btnUpload.addEventListener("click", () => {
        controller.ui.inputUpload.click();
      });
      
      controller.ui.btnSettings.addEventListener("click", () => {
        root.showSettings();
      });
      
      controller.ui.btnToggleTracking.addEventListener("click", () => {
        STATE.toggleTracking();
      });
      
      controller.ui.btnDownload.addEventListener("click", () => {
        STATE.downloadSavefile();
      });
      
      controller.ui.btnReset.addEventListener("click", () => {
        STATE.resetState();
        root.showSettings();
      });
      
      controller.ui.btnClose.addEventListener("click", () => {
        root.hideController();
        window.DHT_LOADED = false;
      });
      
      controller.ui.inputUpload.addEventListener("change", () => {
        for(var file of controller.ui.inputUpload.files){
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
        }

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
      
      settings.styles = DOM.createStyle([
        "#dht-cfg-overlay { position: absolute; left: 0; top: 0; width: 100%; height: 100%; background-color: #000; opacity: 0.5; display: block; z-index: 1000; }",
        "#dht-cfg { position: absolute; left: 50%; top: 50%; width: 800px; height: 326px; margin-left: -400px; margin-top: -187px; padding: 8px; background-color: #fff; z-index: 1001; }",
        "#dht-cfg-note { margin-top: 22px; }"
      ]);
      
      // overlay
      
      settings.overlay = DOM.createElement("div", document.body);
      settings.overlay.id = "dht-cfg-overlay";
      
      settings.overlay.addEventListener("click", () => {
        root.hideSettings();
      });
      
      // main
      
      settings.ele = DOM.createElement("div", document.body);
      settings.ele.id = "dht-cfg";
      
      settings.ele.innerHTML = [
        "<label><input id='dht-cfg-autoscroll' type='checkbox'> Autoscroll</label><br>",
        "<br>",
        "<label>After reaching the first message in channel...</label><br>",
        "<label><input id='dht-cfg-afm-nothing' name='dht-afm' type='radio'> Do Nothing</label><br>",
        "<label><input id='dht-cfg-afm-pause' name='dht-afm' type='radio'> Pause Tracking</label><br>",
        "<label><input id='dht-cfg-afm-switch' name='dht-afm' type='radio'> Switch to Next Channel</label><br>",
        "<br>",
        "<label>After reaching a previously saved message...</label><br>",
        "<label><input id='dht-cfg-asm-nothing' name='dht-asm' type='radio'> Do Nothing</label><br>",
        "<label><input id='dht-cfg-asm-pause' name='dht-asm' type='radio'> Pause Tracking</label><br>",
        "<label><input id='dht-cfg-asm-switch' name='dht-asm' type='radio'> Switch to Next Channel</label><br>",
        "<p id='dht-cfg-note'>",
        "Tracking will only trigger when Discord loads new messages. ",
        "To ensure that all messages are saved, go to your Friends list to avoid loading any messages, refresh the website, and run the script again.",
        "<br><br>",
        "Discord History Tracker will not save newly received messages, it is specifically made to load and save the server/group/DM history.",
        "<br><br>",
        "It is recommended to disable link and image previews to avoid putting unnecessary strain on your browser.",
        "</p>"
      ].join("");
      
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
        STATE.settings.autoscroll = settings.ui.cbAutoscroll.checked;
      });
      
      Object.keys(settings.ui.optsAfterFirstMsg).forEach(key => {
        settings.ui.optsAfterFirstMsg[key].addEventListener("click", () => {
          STATE.settings.afterFirstMsg = key;
        });
      });
      
      Object.keys(settings.ui.optsAfterSavedMsg).forEach(key => {
        settings.ui.optsAfterSavedMsg[key].addEventListener("click", () => {
          STATE.settings.afterSavedMsg = key;
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
