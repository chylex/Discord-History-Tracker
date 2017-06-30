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
      <h1>Discord History Tracker <span>BETA&nbsp;v.2</span></h1>
      <p>Discord History Tracker is a browser script that lets you locally save chat history in your servers, groups, and private conversations.</p>
      <p>When the script is active, it will automatically load history of the selected text channel up to the first message, and let you download it for offline viewing in your browser.</p>
      
      <img src="img/tracker.png" width="851" class="dht">
      
      <h2>How to Save History</h2>
      <h3>...in your browser</h3>
      <p>Add the script as a browser bookmark by clicking <?php include "./build/track_bookmark.html"; ?>. Then open your <a href="https://discordapp.com/channels/@me">Discord Friends list</a> in a new tab (do not use an already opened one), and click the bookmark.</p>
      
      <h3>...in Discord app</h3>
      <p>Click <?php include "./build/track_copyable.html"; ?> to copy the script. Then press <strong>Ctrl+Shift+I</strong> in the Discord app, select <strong>Console</strong>, paste the script into the text box on the bottom and press <strong>Enter</strong> to run it.</p>
      <p>The script may ask you to reload Discord, simply paste and run the script again after the reload (the console stays open).</p>
      
      <h3>What next?</h3>
      <p>When running for the first time, you will see a <strong>Settings</strong> dialog with additional instructions that explain the limitations and recommendations. Please, read them carefully.</p>
      <p>Upload your previously saved file if you have any. By default, Discord History Tracker is set to pause tracking after it reaches a previously saved message to avoid unnecessary history loading. You may also set it to load all channels in the server or your friends list by checking <strong>Switch to Next Channel</strong>.</p>
      <p>Once you have configured everything, click <strong>Start Tracking</strong>, let it run in the background, and download the file when done.</p>
      
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
      <p>Make sure you are using an up-to-date browser. Internet Explorer and old versions of good browsers are not supported.</p>
    </div>
    
    <script type="text/javascript">
      (function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),
      m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)})(window,document,'script','https://www.google-analytics.com/analytics.js','ga');
      ga('create','UA-48632978-5','auto');ga('send','pageview');
      
      document.getElementById("tracker-copy").addEventListener("click", function(){
        var ele = document.getElementById("tracker-copy-contents");
        ele.style.display = "block";
        ele.select();
        document.execCommand("copy");
        ele.style.display = "none";
      });
    </script>
  </body>
</html>