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
      <h1>Discord History Tracker <span class="version">BETA&nbsp;v.9</span>&nbsp;<span class="bar">|</span>&nbsp;<span class="notes"><a href="https://github.com/chylex/Discord-History-Tracker/wiki/Release-Notes">Release&nbsp;Notes</a></span></h1>
      <p>Discord History Tracker is a browser script that lets you locally save chat history in your servers, groups, and private conversations.</p>
      <p>When the script is active, it will automatically load history of the selected text channel up to the first message, and let you download it for offline viewing in your browser.</p>
      
      <img src="img/tracker.png" width="851" class="dht">
      
      <h2>How to Save History</h2>
      <h3>Running the Script</h3>
      <ol>
        <li>Click <a href="javascript:" id="tracker-copy-button">Copy to Clipboard</a> to copy the script<noscript> (requires JavaScript)</noscript></li>
        <li>Open the JavaScript console in your browser or the Discord app by pressing <strong>Ctrl</strong>+<strong>Shift</strong>+<strong>I</strong>, and selecting the <strong>Console</strong> tab</li>
        <li>Paste the script into the console, and press <strong>Enter</strong> to run it</li>
        <li>Press <strong>Ctrl</strong>+<strong>Shift</strong>+<strong>I</strong> again to close the console</li>
      </ol>
      
      <p id="tracker-copy-issue">Your browser may not support copying to clipboard, please try copying the script manually:</p>
      <textarea id="tracker-copy-contents"><?php include "./build/track.html"; ?></textarea>
      
      <p>Please note that it's no longer possible to use DHT as a bookmark. Discord updated their <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP">CSP</a> rules which improve website security, but as a result, browsers will no longer execute bookmark scripts on the website.</p>
      
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
        <a href="https://www.patreon.com/chylex">Support&nbsp;Development&nbsp;on&nbsp;Patreon</a>
      </p>
      
      <h2>Disclaimer</h2>
      <p>Discord History Tracker and the viewer are fully client-side and do not communicate with any servers &ndash; the terms 'Upload' and 'Download' only refer to your browser. If you close your browser while the script is running, all unsaved progress will be lost.</p>
      <p>Please, do not use this script for large or public servers. The script was made as a convenient way of keeping a local copy of private and group chats, as Discord is currently lacking this functionality.</p>
    </div>
    
    <script type="text/javascript">
      (function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),
      m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)})(window,document,'script','https://www.google-analytics.com/analytics.js','ga');
      ga('create','UA-48632978-5','auto');ga('send','pageview');
      
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
