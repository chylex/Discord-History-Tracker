const HTML_ENTITY_MAP = {
	"&": "&amp;",
	"<": "&lt;",
	">": "&gt;",
	"\"": "&quot;",
	"'": "&#39;"
};

const HTML_ENTITY_REGEX = /[&<>"']/g;

class DOM {
	/**
	 * Returns a child element by its ID. Parent defaults to the entire document.
	 */
	static id(id, parent) {
		return (parent || document).getElementById(id);
	}
	
	/**
	 * Returns an array of all child elements containing the specified class. Parent defaults to the entire document.
	 */
	static cls(cls, parent) {
		return Array.prototype.slice.call((parent || document).getElementsByClassName(cls));
	}
	
	/**
	 * Returns an array of all child elements that have the specified tag. Parent defaults to the entire document.
	 */
	static tag(tag, parent) {
		return Array.prototype.slice.call((parent || document).getElementsByTagName(tag));
	}
	
	/**
	 * Returns the first child element containing the specified class. Parent defaults to the entire document.
	 */
	static fcls(cls, parent) {
		return (parent || document).getElementsByClassName(cls)[0];
	}
	
	/**
	 * Converts characters to their HTML entity form.
	 */
	static escapeHTML(html) {
		return String(html).replace(HTML_ENTITY_REGEX, s => HTML_ENTITY_MAP[s]);
	}
	
	/**
	 * Converts a timestamp into a human readable time, using the browser locale.
	 */
	static getHumanReadableTime(timestamp) {
		const date = new Date(timestamp);
		return date.toLocaleDateString() + ", " + date.toLocaleTimeString();
	};
	
	/**
	 * Parses a url string into a URL object and returns it. If the parsing fails, returns null.
	 */
	static tryParseUrl(url) {
		try {
			return new URL(url);
		} catch (ignore) {
			return null;
		}
	}
}
