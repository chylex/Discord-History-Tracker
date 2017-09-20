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
      <h1>Discord History Tracker <span>BETA&nbsp;v.3</span></h1>
      <p>Discord History Tracker is a browser script that lets you locally save chat history in your servers, groups, and private conversations.</p>
      <p>When the script is active, it will automatically load history of the selected text channel up to the first message, and let you download it for offline viewing in your browser.</p>
      
      <img src="img/tracker.png" width="851" class="dht">
      
      <h2>How to Save History</h2>
      <h3>...using your browser</h3>
      <p>In Firefox, click <a rel="sidebar" title="Discord History Tracker" href="<?php include "./build/track.html"; ?>">Add Bookmark</a> and uncheck &laquo;Load this bookmark in the sidebar&raquo;.</p>
      <p>In Chrome, Edge, Safari, and most other browsers, you will need to manually copy the link address of the <a rel="sidebar" title="Discord History Tracker" href="<?php include "./build/track.html"; ?>">tracker script</a>, and use your browser's bookmark manager to add it as a bookmark.</p>
      <p>After adding the script to your bookmarks, open <a href="https://discordapp.com/channels/@me">Discord</a> and click the bookmark to run the script. If you run into any issues, please make sure your browser is up-to-date.</p>
      
      <h3>...using the Discord app</h3>
      <p>Click <a href="javascript:" id="tracker-copy">Copy to Clipboard</a><textarea id="tracker-copy-contents"><?php include "./build/track.html"; ?></textarea> to copy the script<noscript> (requires JavaScript)</noscript>. Then press <strong>Ctrl+Shift+I</strong> in the Discord app, select <strong>Console</strong>, paste the script into the text box at the bottom and press <strong>Enter</strong> to run it. You can then close the console and continue using the script.</p>
      
      <h3>What next?</h3>
      <p>When running for the first time, you will see a <strong>Settings</strong> dialog where you can configure the script's behavior. These settings will be remembered as long as you don't delete cookies in your browser.</p>
      <p>By default, Discord History Tracker is set to pause tracking after it reaches a previously saved message to avoid unnecessary history loading. You may also set it to load all channels in the server or your friends list by selecting <strong>Switch to Next Channel</strong>.</p>
      <p>Once you have configured everything, upload your previously saved file (if you have any), click <strong>Start Tracking</strong>, and let it run in the background. Once done, or after manually pausing it, you can download the generated file.</p>
      
      <h2>How to View History</h2>
      <p>To browse the saved text channels, open the <a href="build/viewer.html">Viewer</a> and upload the file. It is recommended to download the viewer and place it next to your saved files, that way you can view your history offline.</p>
      
      <h2>External Links</h2>
      <p class="links">
        <a href="https://github.com/chylex/Discord-History-Tracker/issues">Issues&nbsp;&amp;&nbsp;Suggestions</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://github.com/chylex/Discord-History-Tracker">GitHub&nbsp;Repository</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://twitter.com/chylexmc">Follow&nbsp;Dev&nbsp;on&nbsp;Twitter</a>&nbsp;&nbsp;&mdash;&nbsp;
        <a href="https://www.patreon.com/chylex">Support&nbsp;Development&nbsp;on&nbsp;Patreon</a>
      </p>
      
      <h2>Planned Features</h2>
      <ul>
        <li>Message filtering and search</li>
        <li>Statistics</li>
        <li><a href="https://github.com/chylex/Discord-History-Tracker/issues">and more...</a></li>
      </ul>
      
      <h2>Disclaimer</h2>
      <p>Discord History Tracker and the viewer are fully client-side and do not communicate with any servers. If you close your browser while the script is running, all unsaved progress will be lost.</p>
      <p>Please, do not use this script for large or public servers. The script was made as a convenient way of keeping a local copy of private and group chats, as Discord is currently lacking this functionality.</p>
    </div>
    
    <script type="text/javascript">
      (function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),
      m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)})(window,document,'script','https://www.google-analytics.com/analytics.js','ga');
      ga('create','UA-48632978-5','auto');ga('send','pageview');
      
      document.getElementById("tracker-copy").addEventListener("click", function(){
        var ele = document.getElementById("tracker-copy-contents");
        ele.style.display = "block";
        ele.select();
        
        try{
          if (!document.execCommand("copy")){
            throw null;
          }
        }catch(e){
          prompt("Press CTRL+C to copy the script:", ele.value);
        }
        
        ele.style.display = "none";
      });
    </script>
  </body>
</html>