class DOM {
	/**
	 * Returns a child element by its ID. Parent defaults to the entire document.
	 * @param {string} id
	 * @param {HTMLElement?} [parent]
	 * @returns {HTMLElement}
	 */
	static id(id, parent) {
		return (parent || document).getElementById(id);
	}
	
	/**
	 * Returns the first child element containing the specified obfuscated class. Parent defaults to the entire document.
	 * @param {string} cls
	 * @param {HTMLElement?} [parent]
	 * @returns {HTMLElement}
	 */
	static queryReactClass(cls, parent) {
		parent = parent || document;
		return parent.querySelector(`[class*="-${cls}"]`) || parent.querySelector(`[class*="${cls}_"]`);
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
}
