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
      "_afterSavedMsg": CONSTANTS.AUTOSCROLL_ACTION_PAUSE,
      "_metaDiscrim": false,
      "_metaDisplay": false
    };
    
    IS_FIRST_RUN = true;
  }
  
  defineTriggeringProperty(root, "autoscroll", loaded._autoscroll);
  defineTriggeringProperty(root, "afterFirstMsg", loaded._afterFirstMsg);
  defineTriggeringProperty(root, "afterSavedMsg", loaded._afterSavedMsg);
  defineTriggeringProperty(root, "metaDiscrim", loaded._metaDiscrim);
  defineTriggeringProperty(root, "metaDisplay", loaded._metaDisplay);
  
  root.onSettingsChanged = function(callback){
    settingsChangedEvents.push(callback);
  };
  
  if (IS_FIRST_RUN){
    saveSettings();
  }
  
  return root;
})();
