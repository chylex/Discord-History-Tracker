<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <meta name="robots" content="index,follow">
    <meta name="author" content="chylex">
    <meta name="description" content="Discord History Tracker - Save history of Discord servers and private conversations">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    
    <title>Discord History Tracker</title>
    
    <link href="style.css" type="text/css" rel="stylesheet">
  </head>
  <body>
    <div class="inner">
      <h1>Discord History Tracker&nbsp;<span class="bar">|</span>&nbsp;<span class="notes"><a href="https://github.com/chylex/Discord-History-Tracker/releases">Release&nbsp;Notes</a></span></h1>
      <p>Discord History Tracker lets you save chat history in your servers, groups, and private conversations, and view it offline.</p>
      <img src="img/tracker.png" width="851" class="dht bordered">
      <p>This page explains how to use the desktop app, which is available for Windows / Linux / Mac.</p>
      <p>If you are looking for the older version of Discord History Tracker which only needs a browser or the Discord app, visit the page for the <a href="https://dht.chylex.com/browser-only">browser-only version</a>, however keep in mind that this version has <strong>significant limitations and fewer features</strong>.</p>
      
      <h2>How to Use</h2>
      <p>The desktop app can be downloaded from <a href="https://github.com/chylex/Discord-History-Tracker/releases">GitHub</a>. Every release includes 4 versions:</p>
      <ul>
        <li><strong>win-x64</strong> is for Windows (64-bit)</li>
        <li><strong>linux-x64</strong> is for Linux (64-bit)</li>
        <li><strong>osx-x64</strong> is for macOS (Intel)</li>
        <li><strong>portable</strong> requires <a href="https://dotnet.microsoft.com/download/dotnet/5.0/runtime" rel="nofollow noopener">.NET 5</a> to be installed, but should run on any operating system supported by .NET</li>
      </ul>
      <p>For the non-portable versions: extract the <strong>DiscordHistoryTracker</strong> file into a folder, and double-click it to launch the app.<br>For the portable version: extract it into a folder, open the folder in a terminal and type: <code>dotnet DiscordHistoryTracker.dll</code></p>
      
      <h3>How to Track Messages</h3>
      <p>The app saves messages into a database file stored on your computer. When you open the app, you are given the option to create a new database file, or open an existing one.</p>
      <p>In the <strong>Tracking</strong> tab, click <strong>Copy Tracking Script</strong> to generate a tracking script that works similarly to the browser-only version of Discord History Tracker, but instead of saving messages in the browser, the tracking script sends them to the app which saves them in the database file.</p>
      <img src="img/app-tracker.png" class="dht bordered" alt="Screenshot of the App (Tracker tab)">
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
  </body>
</html>
