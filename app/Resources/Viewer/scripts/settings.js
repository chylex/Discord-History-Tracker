const SETTINGS = (function() {
	/**
	 * @type {{}}
	 * @property {Function} onSettingsChanged
	 * @property {Boolean} enableImagePreviews
	 * @property {Boolean} enableFormatting
	 * @property {Boolean} enableUserAvatars
	 * @property {Boolean} enableAnimatedEmoji
	 */
	const root = {
		onSettingsChanged(callback) {
			settingsChangedEvents.push(callback);
		}
	};
	
	const settingsChangedEvents = [];
	
	const triggerSettingsChanged = function(property) {
		for (const callback of settingsChangedEvents) {
			callback(property);
		}
	};
	
	const getStorageItem = (property) => {
		try {
			return localStorage.getItem(property);
		} catch (e) {
			console.error(e);
			return null;
		}
	};
	
	const setStorageItem = (property, value) => {
		try {
			localStorage.setItem(property, value);
		} catch (e) {
			console.error(e);
		}
	};
	
	const defineSettingProperty = (property, defaultValue, storageToValue) => {
		const name = "_" + property;
		
		Object.defineProperty(root, property, {
			get: (() => root[name]),
			set: (value => {
				root[name] = value;
				triggerSettingsChanged(property);
				setStorageItem(property, value);
			})
		});
		
		let stored = getStorageItem(property);
		
		if (stored !== null) {
			stored = storageToValue(stored);
		}
		
		root[name] = stored === null ? defaultValue : stored;
	};
	
	const fromBooleanString = (value) => {
		if (value === "true") {
			return true;
		}
		else if (value === "false") {
			return false;
		}
		else {
			return null;
		}
	};
	
	defineSettingProperty("enableImagePreviews", true, fromBooleanString);
	defineSettingProperty("enableFormatting", true, fromBooleanString);
	defineSettingProperty("enableUserAvatars", true, fromBooleanString);
	defineSettingProperty("enableAnimatedEmoji", true, fromBooleanString);
	
	return root;
})();
