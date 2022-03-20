# Welcome

For instructions on how to **use Discord History Tracker**, visit the [official website](https://dht.chylex.com).

To **report an issue or suggestion**, first please see the [issues](https://github.com/chylex/Discord-History-Tracker/issues) page and make sure someone else hasn't already created a similar issue report. If you do find an existing issue, comment on it or add a reaction. Otherwise, either click [New Issue](https://github.com/chylex/Discord-History-Tracker/issues/new), or contact me via email [contact@chylex.com](mailto:contact@chylex.com) or Twitter [@chylexmc](https://twitter.com/chylexmc).

If you are interested in **building from source code**, continue reading the [build instructions](#Build-Instructions) below.

This branch is dedicated to the Discord History Tracker desktop app. If you are looking for the older browser-only version, visit the [master-browser-only](https://github.com/chylex/Discord-History-Tracker/tree/master-browser-only) branch.

# Build Instructions

### Setup

Fork the repository and clone it to your computer (if you've never used git, you can download the [GitHub Desktop](https://desktop.github.com) client to get started quickly).

Folder organization:
* `app/` contains a Visual Studio solution for the desktop app
* `lib/` contains utilities required to build the project
* `web/` contains source code of the [official website](https://dht.chylex.com), which can be used as a template when making your own website

To start editing source code for the desktop app, open `app/DiscordHistoryTracker.sln` in [Visual Studio](https://visualstudio.microsoft.com/downloads/) or [Rider](https://www.jetbrains.com/rider/).

### Building

To build a `Debug` version of the desktop app, there are no additional requirements.

To build a `Release` version of the desktop app, you will need [Python 3](https://www.python.org/downloads), which is used by the build process to launch `app/Resources/minify.py` script.

When creating `Release` builds on systems other than 64-bit Windows, you will also need [Node + npm](https://nodejs.org/en), and [uglify-js](https://www.npmjs.com/package/uglify-js) installed globally (`npm install uglify-js -g`). On 64-bit Windows, both Node and uglify-js are already included in the `lib/` folder for convenience.

To create `Release` builds ready for distribution, run the `app/build.bat` script on Windows, or `app/build.sh` script on other operating systems. This will create self-contained executables for each major operating system, and a portable version that works on all other systems but requires .NET 5 to be installed. All builds are placed in the `app/bin` folder.
