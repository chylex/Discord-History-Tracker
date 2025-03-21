// ==UserScript==
// @name         Discord History Tracker
// @license      MIT
// @namespace    https://chylex.com
// @homepageURL  https://dht.chylex.com/
// @supportURL   https://github.com/chylex/Discord-History-Tracker/issues
// @include      https://discord.com/*
// @run-at       document-idle
// @grant        none
// @noframes
// ==/UserScript==

const interval = window.setInterval(function() {
	if (document.getElementById("dht-userscript-trigger")) {
		return;
	}
	
	const helpIcon = document.querySelector("section[class*='title'] a[href$='://support.discord.com'] > div[class*='clickable'] > svg");
	if (!helpIcon) {
		return;
	}
	
	const helpIconWrapper = helpIcon.parentElement;
	const helpIconLink = helpIconWrapper.parentElement;
	
	helpIconLink.insertAdjacentHTML("beforebegin", `
<div id="dht-userscript-trigger" role="button" aria-label="Connect Discord History Tracker" class="${helpIconWrapper.getAttribute("class")}">
	<svg width="28" height="16" viewBox="0 0 11 6" fill="currentColor" class="${helpIcon.getAttribute("class")}">
		<path d="M3.133,2.848c0,0.355 -0.044,0.668 -0.132,0.937c-0.088,0.27 -0.208,0.495 -0.36,0.677c-0.153,0.181 -0.333,0.319 -0.541,0.412c-0.207,0.092 -0.431,0.139 -0.672,0.139l-1.413,0l0,-4.266l1.265,0c0.27,0 0.519,0.042 0.746,0.124c0.227,0.083 0.423,0.21 0.586,0.382c0.164,0.171 0.291,0.389 0.383,0.654c0.092,0.264 0.138,0.578 0.138,0.941Zm-0.739,0c0,-0.248 -0.028,-0.461 -0.083,-0.639c-0.056,-0.177 -0.133,-0.323 -0.232,-0.437c-0.099,-0.114 -0.217,-0.198 -0.355,-0.253c-0.139,-0.054 -0.292,-0.082 -0.459,-0.082l-0.518,0l0,2.886l0.621,0c0.147,0 0.283,-0.032 0.409,-0.094c0.125,-0.063 0.233,-0.156 0.325,-0.28c0.092,-0.124 0.163,-0.278 0.215,-0.462c0.051,-0.184 0.077,-0.397 0.077,-0.639Z"></path>
		<path d="M5.939,5.013l0,-1.829l-1.523,0l0,1.829l-0.732,0l0,-4.266l0.732,0l0,1.699l1.523,0l0,-1.699l0.733,0l0,4.266l-0.733,0Z"></path>
		<path d="M8.933,1.437l0,3.576l-0.732,0l0,-3.576l-1.13,0l0,-0.69l2.994,0l0,0.69l-1.132,0Z"></path>
	</svg>
</div>
`);
	
	document.getElementById("dht-userscript-trigger").addEventListener("click", onButtonClicked);
	window.clearInterval(interval);
}, 500);

async function onButtonClicked() {
	let code = await tryReadConnectionCodeFromClipboard();
	if (code === null) {
		code = tryReadConnectionCodeFromPrompt();
		if (code === null) {
			alert("Connection Code is not valid.");
			return;
		}
	}
	
	try {
		await loadTrackingScript(code.port, code.token);
	} catch (e) {
		alert("Could not load tracking script:\n" + e);
	}
}

async function tryReadConnectionCodeFromClipboard() {
	if (!(navigator.clipboard && navigator.clipboard.readText)) {
		return null;
	}
	
	try {
		return parseConnectionCode(await navigator.clipboard.readText());
	} catch (e) {
		console.warn("Could not read Connection Code from clipboard", e);
		return null;
	}
}

function tryReadConnectionCodeFromPrompt() {
	const code = prompt("Please enter the Connection Code:");
	return code === null ? null : parseConnectionCode(code);
}

/**
 * @param {string} code
 * @return {?{port: number, token: string}}
 */
function parseConnectionCode(code) {
	if (code.length > 1000) {
		return null;
	}
	
	const regex = /^(\d{1,5}):([a-z0-9]{1,100})$/;
	const match = code.match(regex);
	if (!match) {
		return null;
	}
	
	const port = Number(match[1]);
	if (port < 0 || port > 65536) {
		return null;
	}
	
	return { port, token: match[2] };
}

async function loadTrackingScript(port, token) {
	const response = await fetch("http://127.0.0.1:" + port + "/get-tracking-script?token=" + encodeURIComponent(token));
	
	if (!response.ok) {
		throw "Invalid response";
	}
	
	if (response.headers.get("X-DHT") !== "1") {
		throw response.status + " " + response.statusText;
	}
	
	// noinspection DynamicallyGeneratedCodeJS
	eval(await response.text());
}
