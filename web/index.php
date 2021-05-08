<!DOCTYPE html>
<html lang="en">
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
      <p>Discord History Tracker lets you save chat history in your servers, groups, and private conversations, and view it offline.</p>
      <p>You can use Discord History Tracker either entirely in your browser, or download a desktop app for Windows / Linux / Mac. While the browser-only method is simpler and works on any device that has a modern web browser, it has significant limitations and fewer features than the app. Please read about both methods below.</p>
      
      <img src="img/tracker.png" width="851" class="dht bordered">
      
      <h2>Method 1: Browser-Only</h2>
      <p>A tracking script will load messages according to your settings, and save them in your browser.</p>
      <p>Because everything happens in your browser, if the browser tab is closed, or your browser or computer crashes, you will lose all progress. Your browser may also be unable to process large amounts of messages. If this is a concern, use the app method.</p>
      
      <h3>Setup the Tracking Script</h3>
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
      
      <h4>Option 2: Browser / Discord Console</h4>
      <div class="quote">
        <p>The console is the only way to use DHT directly in the desktop app.</p>
        
        <ol>
          <li>Click <a href="javascript:" id="tracker-copy-button" onauxclick="return false;">Copy to Clipboard</a> to copy the tracking script
            <noscript> (requires JavaScript)</noscript>
          </li>
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
      <p>Whenever DHT is updated to work with a new version of Discord, it may no longer work with the previous version of Discord.</p>
      <p>If you haven't received that Discord update yet, see <a href="https://github.com/chylex/Discord-History-Tracker/wiki/Release-Notes">Release Notes</a> for information about recent updates, and <a href="https://github.com/chylex/Discord-History-Tracker/wiki/Old-Versions">Old Versions</a> if you need to use an older version of DHT.</p>
      
      <h3>How to Track Messages</h3>
      <p>When using the script for the first time, you will see a <strong>Settings</strong> dialog where you can configure the script. These settings will be remembered as long as you don't delete cookies in your browser.</p>
      <p>By default, Discord History Tracker is set to automatically scroll up to load the channel history, and pause tracking if it reaches a previously saved message to avoid unnecessary history loading.</p>
      <p>Before you <strong>Start Tracking</strong>, you may use <strong>Upload &amp; Combine</strong> to load messages from a previously saved archive file into the browser.</p>
      <p>When you click <strong>Download</strong>, the browser will generate an archive file from saved messages, and lets you save it to your computer.</p>
      
      <h3>How to View History</h3>
      <p>First, save the <a href="build/viewer.html">Viewer</a> file to your computer. Then you can open the downloaded viewer in your browser, click <strong>Load File</strong>, and select the archive to view.</p>
      
      <h2>Method 2: Desktop App</h2>
      <p>The app can be downloaded from <a href="https://github.com/chylex/Discord-History-Tracker/releases">GitHub</a>. Every release includes 4 versions available:</p>
      <ul>
        <li><strong>win-x64</strong> is for Windows (64-bit)</li>
        <li><strong>linux-x64</strong> is for Linux (64-bit)</li>
        <li><strong>osx-x64</strong> is for macOS (Intel)</li>
        <li><strong>portable</strong> requires <a href="https://dotnet.microsoft.com/download/dotnet/5.0/runtime" rel="nofollow noopener">.NET 5</a> to be installed, but should run on any operating system supported by .NET</li>
      </ul>
      <p>The three non-portable versions include an executable named <strong>DiscordHistoryTracker</strong> you can launch. For the portable version, extract the archive into a folder, open the folder in a terminal and type: <code>dotnet DiscordHistoryTracker.dll</code></p>
      
      <h3>How to Track Messages</h3>
      <p>The app saves messages into a database file stored on your computer. When you open the app, you are given the option to create a new database file, or open an existing one.</p>
      <p>In the <strong>Tracking</strong> tab, click <strong>Copy Tracking Script</strong> to generate a tracking script that works similarly to the browser-only version of Discord History Tracker, but instead of saving messages in the browser, the tracking script sends them to the app which saves them in the database file.</p>
      <img src="img/app-tracker.png" class="dht bordered" alt="Screenshot of the App (Tracker tab)">
      <p>See <strong>Option 2: Browser / Discord Console</strong> above for more detailed instructions on how to paste the tracking script into the browser or Discord app console.</p>
      <p>When using the script for the first time, you will see a <strong>Settings</strong> dialog where you can configure the script. These settings will be remembered as long as you don't delete cookies in your browser.</p>
      <p>By default, Discord History Tracker is set to automatically scroll up to load the channel history, and pause tracking if it reaches a previously saved message to avoid unnecessary history loading.</p>
      
      <h3>How to View History</h3>
      <p>In the <strong>Viewer</strong> tab, you can open a viewer in your browser, or save it as a file you can open in your browser later. You also have the option to apply filters to only view a portion of the saved messages.</p>
      <img src="img/app-viewer.png" class="dht bordered" alt="Screenshot of the App (Viewer tab)">
      
      <h3>Technical Details</h3>
      <ol>
        <li>The app uses SQLite, which means that you can use SQL to manually query or manipulate the database file.</li>
        <li>The app communicates with the script using an integrated server. The server only listens for local connections (i.e. connections from programs running on your computer, not the internet). When you copy the tracking script, it will contain a randomly generated token that ensures only the tracking script is able to talk to the server.</li>
        <li>You can use the <code>-port &lt;p&gt;</code> and <code>-token &lt;t&gt;</code> command line arguments to configure the server manually &mdash; otherwise, they will be assigned automatically in a way that allows running multiple separate instances of the app.</li>
      </ol>
      
      <h2>External Links</h2>
      <p class="links">
        <a href="https://github.com/chylex/Discord-History-Tracker/issues">Issues&nbsp;&amp;&nbsp;Suggestions</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://github.com/chylex/Discord-History-Tracker">Source&nbsp;Code</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://twitter.com/chylexmc">Follow&nbsp;Dev&nbsp;on&nbsp;Twitter</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://www.patreon.com/chylex">Support&nbsp;via&nbsp;Patreon</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://ko-fi.com/chylex">Support&nbsp;via&nbsp;Ko-fi</a>
      </p>
    </div>
    
    <script type="text/javascript">
      var contents = document.getElementById("tracker-copy-contents");
      var issue = document.getElementById("tracker-copy-issue");
      var button = document.getElementById("tracker-copy-button");
      
      if (document.queryCommandSupported("copy")) {
        contents.style.display = "none";
        issue.style.display = "none";
      }
      
      button.addEventListener("click", function() {
        contents.style.display = "block";
        issue.style.display = "block";
        
        contents.select();
        document.execCommand("copy");
        
        button.innerHTML = "Copied to Clipboard";
        contents.style.display = "none";
        issue.style.display = "none";
      });
      
      contents.addEventListener("click", function() {
        contents.select();
      });
    </script>
  </body>
</html>
