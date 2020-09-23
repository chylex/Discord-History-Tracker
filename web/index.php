<!DOCTYPE html>
<html>
  <head>
    <meta charset="utf-8">
    <meta name="robots" content="index,follow">
    <meta name="author" content="chylex">
    <meta name="description" content="Discord History Tracker - Browser script to save history of Discord servers and private conversations">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    
    <title>Discord History Tracker</title>
    
    <link href="style.css" type="text/css" rel="stylesheet">
  </head>
  <body>
    <div class="inner">
      <h1>Discord History Tracker <span class="version">{{{version:web}}}</span>&nbsp;<span class="bar">|</span>&nbsp;<span class="notes"><a href="https://github.com/chylex/Discord-History-Tracker/wiki/Release-Notes">Release&nbsp;Notes</a></span></h1>
      <p>Discord History Tracker is a browser script that lets you locally save chat history in your servers, groups, and private conversations.</p>
      <p>When the script is active, it will load history of the selected text channel up to the first message, and let you download it for offline viewing in your browser.</p>
      
      <img src="img/tracker.png" width="851" class="dht bordered">
      
      <h2>How to Save History</h2>
      <h3>Running the Script</h3>
      
      <h4>Option 1: Userscript</h4>
      <div class="quote">
        <p><strong>Preferred option.</strong> Requires a browser addon, but DHT will stay up-to-date and be easily accessible on the Discord website.</p>
        
        <ol>
          <li>Install a userscript manager addon:
            <ul>
              <li><a href="https://violentmonkey.github.io/get-it/">Violentmonkey</a> (Chrome)</li>
              <li><a href="https://tampermonkey.net/">Tampermonkey</a> (Firefox, Edge, Chrome, Opera)</li>
              <li>Due to browser bugs / limitations, DHT will not work in <strong>Firefox</strong> with Greasemonkey / Violentmonkey, and in <strong>Safari</strong> at all</li>
            </ul>
          </li>
          <li>Click <a href="build/track.user.js">Install Userscript</a> to prompt an installation into the userscript manager</li>
          <li>Open <a href="https://discord.com/channels/@me" rel="noreferrer">Discord</a>, and view any server, group, or private conversation (it will not appear in Friends list)</li>
          <li>Click <strong>DHT</strong> in the top right corner:<br><img src="img/button.png" class="bordered"></li>
        </ol>
      </div>
      
      <h4>Option 2: Browser Console</h4>
      <div class="quote">
        <p>The console is the only way to use DHT directly in the desktop app.</p>
        
        <ol>
          <li>Click <a href="javascript:" id="tracker-copy-button" onauxclick="return false;">Copy to Clipboard</a> to copy the script<noscript> (requires JavaScript)</noscript></li>
          <li>Press <strong>Ctrl</strong>+<strong>Shift</strong>+<strong>I</strong> in your browser or the Discord app, and select the <strong>Console</strong> tab</li>
          <li>Paste the script into the console, and press <strong>Enter</strong> to run it</li>
          <li>Press <strong>Ctrl</strong>+<strong>Shift</strong>+<strong>I</strong> again to close the console</li>
        </ol>
        
        <p id="tracker-copy-issue">Your browser may not support copying to clipboard, please try copying the script manually:</p>
        <textarea id="tracker-copy-contents"><?php include "./build/track.html"; ?></textarea>
      </div>
      
      <h4>Option 3: Bookmarklet</h4>
      <div class="quote">
        <p>Requires Firefox 69 or newer.</p>
        
        <ol>
          <li>Right-click <a href="<?php include "./build/track.html"; ?>" onclick="return false;" onauxclick="return false;">Discord History Tracker</a></li>
          <li>Select &laquo;Bookmark This Link&raquo; and save the bookmark</li>
          <li>Open <a href="https://discord.com/channels/@me" rel="noreferrer">Discord</a> and click the bookmark to run the script</li>
        </ol>
      </div>
      
      <h4>Old Versions</h4>
      <div class="quote">
        <p>Whenever DHT is fixed to work with a recent Discord update, it will no longer work on the previous version of Discord.</p>
        <p>If you haven't received that Discord update yet, see <a href="https://github.com/chylex/Discord-History-Tracker/wiki/Release-Notes">Release Notes</a> for information about recent updates, and <a href="https://github.com/chylex/Discord-History-Tracker/wiki/Old-Versions">Old Versions</a> if you need to use an older version of DHT.</p>
      </div>
      
      <h3>Using the Script</h3>
      <p>When running for the first time, you will see a <strong>Settings</strong> dialog where you can configure the script. These settings will be remembered as long as you don't delete cookies in your browser.</p>
      <p>By default, Discord History Tracker is set to pause tracking after it reaches a previously saved message to avoid unnecessary history loading. You may also set it to load all channels in the server or your friends list by selecting <strong>Switch to Next Channel</strong>.</p>
      <p>Once you have configured everything, upload your previously saved archive (if you have any), click <strong>Start Tracking</strong>, and let it run. After the script saves all messages, download the archive.</p>
      
      <h2>How to View History</h2>
      <p>Download the <a href="build/viewer.html">Viewer</a>, open it in your browser, and load the archive. By downloading it to your computer, you can view archives offline, and allow the browser to load image previews that might otherwise not load if the remote server prevents embedding them.</p>
      
      <h2>External Links</h2>
      <p class="links">
        <a href="https://github.com/chylex/Discord-History-Tracker/issues">Issues&nbsp;&amp;&nbsp;Suggestions</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://github.com/chylex/Discord-History-Tracker">Source&nbsp;Code</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://twitter.com/chylexmc">Follow&nbsp;Dev&nbsp;on&nbsp;Twitter</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://www.patreon.com/chylex">Support&nbsp;via&nbsp;Patreon</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://ko-fi.com/chylex">Support&nbsp;via&nbsp;Ko-fi</a>
      </p>
      
      <h2>Disclaimer</h2>
      <p>Discord History Tracker and the viewer are fully client-side and do not communicate with any servers &ndash; the terms 'Upload' and 'Download' only refer to your browser. If you close your browser while the script is running, all unsaved progress will be lost.</p>
      <p>Please, do not use this script for large or public servers. The script was made as a convenient way of keeping a local copy of private and group chats, as Discord is currently lacking this functionality.</p>
    </div>
    
    <script type="text/javascript">
      var contents = document.getElementById("tracker-copy-contents");
      var issue = document.getElementById("tracker-copy-issue");
      var button = document.getElementById("tracker-copy-button");
      
      if (document.queryCommandSupported("copy")){
        contents.style.display = "none";
        issue.style.display = "none";
      }
      
      button.addEventListener("click", function(){
        contents.style.display = "block";
        issue.style.display = "block";
        
        contents.select();
        document.execCommand("copy");
        
        button.innerHTML = "Copied to Clipboard";
        contents.style.display = "none";
        issue.style.display = "none";
      });
      
      contents.addEventListener("click", function(){
        contents.select();
      })
    </script>
  </body>
</html>
