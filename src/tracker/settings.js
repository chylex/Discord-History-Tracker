var CONSTANTS = {
  AUTOSCROLL_ACTION_NOTHING: "optNothing",
  AUTOSCROLL_ACTION_PAUSE: "optPause",
  AUTOSCROLL_ACTION_SWITCH: "optSwitch"
};

var SETTINGS = (function(){
  var root = {};
  var settingsChangedEvents = [];
  
  var triggerSettingsChanged = function(changeType, changeDetail){
    for(var callback of settingsChangedEvents){
      callback(changeType, changeDetail);
    }
    
    DOM.saveToCookie("DHT_SETTINGS", root, 60*60*24*365*5);
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
  
  var loaded = DOM.loadFromCookie("DHT_SETTINGS") || {
    "_autoscroll": true,
    "_afterFirstMsg": CONSTANTS.AUTOSCROLL_ACTION_PAUSE,
    "_afterSavedMsg": CONSTANTS.AUTOSCROLL_ACTION_PAUSE
  };
  
  defineTriggeringProperty(root, "autoscroll", loaded._autoscroll);
  defineTriggeringProperty(root, "afterFirstMsg", loaded._afterFirstMsg);
  defineTriggeringProperty(root, "afterSavedMsg", loaded._afterSavedMsg);
  
  root.onSettingsChanged = function(callback){
    settingsChangedEvents.push(callback);
  };
  
  return root;
})();
