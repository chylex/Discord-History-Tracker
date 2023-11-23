const CONSTANTS = {
	AUTOSCROLL_ACTION_NOTHING: "optNothing",
	AUTOSCROLL_ACTION_PAUSE: "optPause",
	AUTOSCROLL_ACTION_SWITCH: "optSwitch"
};

let IS_FIRST_RUN = false;

const SETTINGS = (function() {
	const settingsChangedEvents = [];
	
	const saveSettings = function() {
		DOM.saveToCookie("DHT_SETTINGS", root, 60 * 60 * 24 * 365 * 5);
	};
	
	const triggerSettingsChanged = function(property) {
		for (const callback of settingsChangedEvents) {
			callback(property);
		}
		
		saveSettings();
	};
	
	const defineTriggeringProperty = function(obj, property, value) {
		const name = "_" + property;
		
		Object.defineProperty(obj, property, {
			get: (() => obj[name]),
			set: (value => {
				obj[name] = value;
				triggerSettingsChanged(property);
			})
		});
		
		obj[name] = value;
	};
	
	let loaded = DOM.loadFromCookie("DHT_SETTINGS");
	
	if (!loaded) {
		loaded = {
			"_autoscroll": true,
			"_afterFirstMsg": CONSTANTS.AUTOSCROLL_ACTION_PAUSE,
			"_afterSavedMsg": CONSTANTS.AUTOSCROLL_ACTION_PAUSE
		};
		
		IS_FIRST_RUN = true;
	}
	
	const root = {
		onSettingsChanged(callback) {
			settingsChangedEvents.push(callback);
		}
	};
	
	defineTriggeringProperty(root, "autoscroll", loaded._autoscroll);
	defineTriggeringProperty(root, "afterFirstMsg", loaded._afterFirstMsg);
	defineTriggeringProperty(root, "afterSavedMsg", loaded._afterSavedMsg);
	
	if (IS_FIRST_RUN) {
		saveSettings();
	}
	
	return root;
})();
