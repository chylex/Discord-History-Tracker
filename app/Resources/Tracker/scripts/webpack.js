/**
 * Parts copied from Better Discord, licensed under Apache License 2.0.
 *
 * https://github.com/BetterDiscord/BetterDiscord/blob/2752daf64f98625fc67c569361bd56021307d058/renderer/src/modules/webpackmodules.js
 * https://github.com/BetterDiscord/BetterDiscord/blob/2752daf64f98625fc67c569361bd56021307d058/LICENSE
 * 
 * https://github.com/BetterDiscord/BetterDiscord/blob/1b859560148a908f077278fba9625ea82e670222/src/betterdiscord/webpack/shared.ts
 * https://github.com/BetterDiscord/BetterDiscord/blob/1b859560148a908f077278fba9625ea82e670222/LICENSE
 */

// noinspection RedundantIfStatementJS
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
		
		const id = Symbol("dht-webpackmodules-" + new Date().getTime());
		if (typeof (window["webpackChunkdiscord_app"]) !== "undefined") {
			window["webpackChunkdiscord_app"].push([ [ id ], {}, internalRequire => {
				if ("b" in internalRequire) {
					hookedRequire = internalRequire;
				}
			}]);
		}
		
		return this._require = hookedRequire;
	}
	
	static getAllModules() {
		return this.require.c;
	}
	
	static shouldSkipModule(exports) {
		if (!(typeof exports === "object" || typeof exports === "function")) {
			return true;
		}
		if (!exports) {
			return true;
		}
		if (exports === window) {
			return true;
		}
		if (exports === document.documentElement) {
			return true;
		}
		if (exports[Symbol.toStringTag] === "DOMTokenList") {
			return true;
		}
		if (exports === Symbol) {
			return true;
		}
		if (exports instanceof Window) {
			return true;
		}
		return false;
	}
	
	static filterByProps(...props) {
		return module => props.every(prop => prop in module);
	}
	
	static filterByPropsWithPredicate(predicate, ...props) {
		return module => props.every(prop => prop in module && predicate(module[prop]));
	}
	
	static findModules(filter) {
		const moduleFilter = module => (typeof module === "object" || typeof module === "function") && filter(module);
		
		const results = [];
		
		for (const module of Object.values(this.getAllModules())) {
			/**
			 * @type {Object} Exports
			 * @property [A]
			 * @property [Ay]
			 * @property [default]
			 */
			const exports = module.exports;
			if (this.shouldSkipModule(exports)) {
				continue;
			}
			
			let foundModule;
			if (exports.A && moduleFilter(exports.A)) {
				foundModule = exports.A;
			}
			else if (exports.Ay && moduleFilter(exports.Ay)) {
				foundModule = exports.Ay;
			}
			else if (exports.default && moduleFilter(exports.default)) {
				foundModule = exports.default;
			}
			else if (moduleFilter(exports)) {
				foundModule = exports;
			}
			else {
				continue;
			}
			
			results.push(foundModule);
		}
		
		return results;
	}
	
	static findModule(name, filter) {
		const modules = this.findModules(filter);
		if (modules.length === 1) {
			return modules[0];
		}
		
		console.error("[DHT] Cannot find module " + name + ", results found:", modules.length, modules);
		return null;
	}
	
	static findFunction(name, additionalRequiredProps) {
		const searchedProps = additionalRequiredProps ? [ name, ...additionalRequiredProps ] : [ name ];
		const matchingModule = this.findModule("containing function " + name, this.filterByPropsWithPredicate(prop => typeof (prop) === "function", ...searchedProps));
		return matchingModule == null ? null : matchingModule[name].bind(matchingModule);
	}
}
