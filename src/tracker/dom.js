class DOM {
	/**
	 * Returns a child element by its ID. Parent defaults to the entire document.
	 * @returns {HTMLElement}
	 */
	static id(id, parent) {
		return (parent || document).getElementById(id);
	}
	
	/**
	 * Returns the first child element containing the specified obfuscated class. Parent defaults to the entire document.
	 */
	static queryReactClass(cls, parent) {
		return (parent || document).querySelector(`[class*="${cls}_"]`);
	}
	
	/**
	 * Creates an element, adds it to the DOM, and returns it.
	 */
	static createElement(tag, parent, id, html) {
		/** @type HTMLElement */
		const ele = document.createElement(tag);
		ele.id = id || "";
		ele.innerHTML = html || "";
		parent.appendChild(ele);
		return ele;
	}
	
	/**
	 * Removes an element from the DOM.
	 */
	static removeElement(ele) {
		return ele.parentNode.removeChild(ele);
	}
	
	/**
	 * Creates a new style element with the specified CSS and returns it.
	 */
	static createStyle(styles) {
		return this.createElement("style", document.head, "", styles);
	}
	
	/**
	 * Utility function to save an object into a cookie.
	 */
	static saveToCookie(name, obj, expiresInSeconds) {
		const expires = new Date(Date.now() + 1000 * expiresInSeconds).toUTCString();
		document.cookie = name + "=" + encodeURIComponent(JSON.stringify(obj)) + ";path=/;expires=" + expires;
	}
	
	/**
	 * Utility function to load an object from a cookie.
	 */
	static loadFromCookie(name) {
		const value = document.cookie.replace(new RegExp("(?:(?:^|.*;\\s*)" + name + "\\s*\\=\\s*([^;]*).*$)|^.*$"), "$1");
		return value.length ? JSON.parse(decodeURIComponent(value)) : null;
	}
	
	/**
	 * Returns internal React state object of an element.
	 */
	static getReactProps(ele) {
		const keys = Object.keys(ele || {});
		let key = keys.find(key => key.startsWith("__reactInternalInstance"));
		
		if (key) {
			// noinspection JSUnresolvedVariable
			return ele[key].memoizedProps;
		}
		
		key = keys.find(key => key.startsWith("__reactProps$"));
		return key ? ele[key] : null;
	}
	
	/**
	 * Returns internal React state object of an element, or null if the retrieval throws.
	 */
	static tryGetReactProps(ele) {
		try {
			return this.getReactProps(ele);
		} catch (e) {
			return null;
		}
	}
}
