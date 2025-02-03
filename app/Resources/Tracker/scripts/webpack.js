/**
 * Parts copied from Better Discord, licensed under Apache License 2.0.
 *
 * https://github.com/BetterDiscord/BetterDiscord/blob/78edeb77c60542a57884686c4ba98f997c886fad/renderer/src/modules/webpackmodules.js
 * https://github.com/BetterDiscord/BetterDiscord/blob/78edeb77c60542a57884686c4ba98f997c886fad/LICENSE
 */
class WEBPACK {
	static get require() {
		if (this._require) {
			return this._require;
		}
		
		/**
		 * @type {Object}
		 * @property {Object} m
		 * @property {Object} c
		 */
		let hookedRequire;
		
		const id = "dht-webpackmodules-" + new Date().getTime();
		if (typeof (window["webpackChunkdiscord_app"]) !== "undefined") {
			window["webpackChunkdiscord_app"].push([ [ id ], {}, internalRequire => hookedRequire = internalRequire ]);
		}
		
		delete hookedRequire.m[id];
		delete hookedRequire.c[id];
		return this._require = hookedRequire;
	}
	
	static getAllModules() {
		return this.require.c;
	}
	
	static filterByProps(...props) {
		return module => props.every(prop => prop in module);
	}
	
	static filterByPropsWithPredicate(predicate, ...props) {
		return module => props.every(prop => prop in module && predicate(module[prop]));
	}
	
	static findModules(filter) {
		const defaultExport = true;
		const moduleFilter = module => (typeof module === "object" || typeof module === "function") && filter(module);
		
		const results = [];
		
		for (const module of Object.values(this.getAllModules())) {
			/**
			 * @type {Object}
			 * @property [Z]
			 * @property [ZP]
			 * @property [__esModule]
			 * @property [default]
			 */
			const exports = module.exports;
			if (!exports || exports === window || exports === document.documentElement || exports[Symbol.toStringTag] === "DOMTokenList") {
				continue;
			}
			
			let foundModule = null;
			if (exports.Z && moduleFilter(exports.Z)) {
				foundModule = defaultExport ? exports.Z : exports;
			}
			if (exports.ZP && moduleFilter(exports.ZP)) {
				foundModule = defaultExport ? exports.ZP : exports;
			}
			if (exports.__esModule && exports.default && moduleFilter(exports.default)) {
				foundModule = defaultExport ? exports.default : exports;
			}
			if (moduleFilter(exports)) {
				foundModule = exports;
			}
			
			if (foundModule) {
				results.push(foundModule);
			}
		}
		
		return results;
	}
	
	static findModule(name, filter) {
		const modules = this.findModules(filter);
		if (modules.length === 1) {
			return modules[0];
		}
		
		console.error("[DHT] Cannot find module " + name + ", results found:", modules.length);
		return null;
	}
	
	static findFunction(name, additionalRequiredProps) {
		const searchedProps = additionalRequiredProps ? [ name, ...additionalRequiredProps ] : [ name ];
		const matchingModule = this.findModule("containing function " + name, this.filterByPropsWithPredicate(prop => typeof (prop) === "function", ...searchedProps));
		return matchingModule == null ? null : matchingModule[name].bind(matchingModule);
	}
}
