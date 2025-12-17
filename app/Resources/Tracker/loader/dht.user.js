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

function startMutationObserver(callback) {
	let hasDebounceStarted = false;
	
	function handleMutations(mutations, observer) {
		if (hasDebounceStarted) {
			return;
		}
		
		hasDebounceStarted = true;
		window.setTimeout(() => {
			hasDebounceStarted = false;
			callback(observer);
		}, 20);
	}
	
	new MutationObserver(handleMutations).observe(document.body, { childList: true, subtree: true });
}

startMutationObserver(observer => {
	const triggerButton = installConnectDialogButton();
	if (triggerButton === null) {
		return;
	}
	
	observer.disconnect();
	
	startMutationObserver(() => {
		if (!triggerButton.isConnected) {
			getHelpIcon()?.parentElement.parentElement.insertAdjacentElement("afterend", triggerButton);
		}
	});
});

function installConnectDialogButton() {
	const helpIcon = getHelpIcon();
	if (!helpIcon) {
		return null;
	}
	
	const helpIconWrapper = helpIcon.parentElement;
	const helpIconLink = helpIconWrapper.parentElement;
	
	helpIconLink.insertAdjacentHTML("afterend", `
<div id="dht-connector-show-dialog" role="button" class="${helpIconWrapper.getAttribute("class")}">
  <svg viewBox="0 0 24 20" fill="currentColor" class="${helpIcon.getAttribute("class")}" style="width: auto;">
    <path d="M7.446,9.924c-0,0.846 -0.105,1.593 -0.315,2.234c-0.21,0.644 -0.497,1.181 -0.86,1.615c-0.365,0.431 -0.795,0.761 -1.292,0.982c-0.494,0.22 -1.029,0.332 -1.605,0.332l-3.374,-0l0,-10.174l3.021,0c0.645,0 1.239,0.1 1.781,0.296c0.542,0.198 1.011,0.501 1.4,0.911c0.391,0.408 0.695,0.928 0.914,1.56c0.22,0.629 0.33,1.378 0.33,2.244Zm-1.765,-0c0,-0.592 -0.067,-1.1 -0.198,-1.524c-0.134,-0.422 -0.318,-0.77 -0.554,-1.042c-0.236,-0.272 -0.518,-0.473 -0.848,-0.604c-0.332,-0.128 -0.697,-0.195 -1.096,-0.195l-1.237,-0l0,6.882l1.483,0c0.351,0 0.676,-0.076 0.977,-0.224c0.298,-0.15 0.556,-0.372 0.776,-0.668c0.22,-0.295 0.389,-0.663 0.513,-1.101c0.122,-0.439 0.184,-0.947 0.184,-1.524Z"/>
    <path d="M14.147,15.087l-0,-4.362l-3.637,-0l-0,4.362l-1.748,-0l-0,-10.174l1.748,0l-0,4.052l3.637,-0l-0,-4.052l1.75,0l0,10.174l-1.75,-0Z"/>
    <path d="M21.297,6.559l-0,8.528l-1.748,-0l-0,-8.528l-2.699,-0l0,-1.646l7.15,0l0,1.646l-2.703,-0Z"/>
  </svg>
</div>
`);
	
	const button = document.getElementById("dht-connector-show-dialog");
	button.addEventListener("click", showConnectDialog);
	return button;
}

function getHelpIcon() {
	return document.querySelector("div[class*='bar'] a[href$='://support.discord.com'] > div[class*='clickable'] > svg");
}

function showConnectDialog() {
	if (window.DHT_LOADED) {
		alert("Discord History Tracker is already loaded.");
		return;
	}
	
	const dialogElement = document.createElement("dialog");
	dialogElement.id = "dht-connector-dialog";
	dialogElement.innerHTML = `
<form id="dht-connector-dialog-form">
  <label for="dht-connector-dialog-input-code">Connection Code</label>
  <input id="dht-connector-dialog-input-code" type="text">
  <p id="dht-connector-dialog-input-error"></p>
  
  <div id="dht-connector-dialog-buttons">
    <button type="submit" id="dht-connector-dialog-button-connect">Connect</button>
    <button type="button" id="dht-connector-dialog-button-close">Cancel</button>
  </div>
</form>
<style>
  #dht-connector-dialog {
    width: 300px;
    padding: 18px;
    font-size: 16px;
    border: none;
    border-radius: 8px;
    background-color: #313338;
  }
  
  #dht-connector-dialog::backdrop {
    background-color: rgba(0, 0, 0, 0.6);
  }
  
  #dht-connector-dialog label {
    display: block;
    margin: 0 0 9px;
    font-weight: 500;
    letter-spacing: .02em;
    color: #fbfbfb;
  }
  
  #dht-connector-dialog-input-error {
    margin: 11px 1px;
    color: #ff484c;
    font-size: 15px;
    font-weight: 500;
  }
  
  #dht-connector-dialog input[type="text"] {
    box-sizing: border-box;
    width: 100%;
    padding: 12px 10px;
    border: 1px solid rgba(151, 151, 159, 0.2);
    border-radius: 8px;
    color: #efeff0;
    background-color: #1d1d21;
  }
  
  #dht-connector-dialog-buttons {
    margin-top: 15px;
    display: flex;
    justify-content: end;
    gap: 12px;
  }
  
  #dht-connector-dialog button {
    padding: 8px 16px;
    border: none;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 500;
    color: #fff;
    background-color: #5865f2;
    transition: background-color 200ms ease, color 200ms ease;
  }
  
  #dht-connector-dialog button:hover {
    background-color: #4752c4;
  }
  
  #dht-connector-dialog button[disabled] {
    cursor: not-allowed;
    opacity: 0.5;
  }
</style>
	`;
	
	document.body.insertAdjacentElement("beforeend", dialogElement);
	
	const formElement = document.getElementById("dht-connector-dialog-form");
	const codeErrorElement = document.getElementById("dht-connector-dialog-input-error");
	const codeInputElement = document.getElementById("dht-connector-dialog-input-code");
	const connectButtonElement = document.getElementById("dht-connector-dialog-button-connect");
	const closeButtonElement = document.getElementById("dht-connector-dialog-button-close");
	
	dialogElement.addEventListener("close", function() {
		dialogElement.remove();
	});
	
	closeButtonElement.addEventListener("click", function() {
		dialogElement.close();
	});
	
	codeInputElement.addEventListener("paste", function() {
		setTimeout(async function() {
			const code = parseConnectionCode(codeInputElement.value);
			if (code !== null) {
				await onSubmit(code);
			}
		}, 0);
	});
	
	formElement.addEventListener("submit", async function(e) {
		e.preventDefault();
		
		const code = parseConnectionCode(codeInputElement.value);
		if (code === null) {
			codeErrorElement.innerText = "Code is not valid.";
		}
		else {
			await onSubmit(code);
		}
	});
	
	let isSubmitting = false;
	
	async function onSubmit(code) {
		if (isSubmitting) {
			return;
		}
		
		isSubmitting = true;
		connectButtonElement.setAttribute("disabled", "");
		try {
			await loadTrackingScript(code);
		} catch (e) {
			onError("Could not load tracking script.", e);
		} finally {
			isSubmitting = false;
			connectButtonElement.removeAttribute("disabled");
		}
	}
	
	async function loadTrackingScript(code) {
		const trackingScript = await getTrackingScript(code.port, code.token);
		
		if (dialogElement.isConnected) {
			// noinspection DynamicallyGeneratedCodeJS
			eval(trackingScript);
			dialogElement.close();
		}
	}
	
	function onError(message, e) {
		console.error(message, e);
		codeErrorElement.innerText = message;
	}
	
	dialogElement.showModal();
}

/**
 * @param {string} code
 * @return {?{port: number, token: string}}
 */
function parseConnectionCode(code) {
	if (code.length > 5 + 1 + 100) {
		return null;
	}
	
	const match = code.match(/^(\d{1,5}):([a-zA-Z0-9]{1,100})$/);
	if (!match) {
		return null;
	}
	
	const port = Number(match[1]);
	if (port < 0 || port > 65535) {
		return null;
	}
	
	return { port, token: match[2] };
}

async function getTrackingScript(port, token) {
	const url = "http://127.0.0.1:" + port + "/get-tracking-script?token=" + encodeURIComponent(token);
	const response = await fetch(url, {
		credentials: "omit",
		signal: AbortSignal.timeout(2000),
	});
	
	if (!response.ok) {
		throw response.status + " " + response.statusText;
	}
	
	if (response.headers.get("X-DHT") !== "1") {
		throw "Invalid response";
	}
	
	return await response.text();
}
