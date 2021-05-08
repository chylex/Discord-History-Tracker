const TEMPLATE_REGEX = /{([^{}]+?)}/g;

class TEMPLATE {
	constructor(contents) {
		this.contents = contents;
	};
	
	apply(obj, processor) {
		return this.contents.replace(TEMPLATE_REGEX, (full, match) => {
			const value = match.split(".").reduce((o, property) => o[property], obj);
			
			if (processor) {
				const updated = processor(match, value);
				return typeof updated === "undefined" ? DOM.escapeHTML(value) : updated;
			}
			
			return DOM.escapeHTML(value);
		});
	}
}
