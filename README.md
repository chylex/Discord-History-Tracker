# Welcome

All you need to **use Discord History Tracker** is either an up-to-date browser, or the [Discord desktop client](https://discord.com/download). Visit the [official website](https://dht.chylex.com) for instructions.

To **report an issue or suggestion**, first please see the [issues](https://github.com/chylex/Discord-History-Tracker/issues) page and make sure someone else hasn't already created a similar issue report. If you do find an existing issue, comment on it or add a reaction. Otherwise, either click [New Issue](https://github.com/chylex/Discord-History-Tracker/issues/new), or contact me via email [contact@chylex.com](mailto:contact@chylex.com) or Twitter [@chylexmc](https://twitter.com/chylexmc).

If you are interested in **creating your own version** from the source code, continue reading the [build instructions](#Build-Instructions) below.

# Build Instructions

Follow the steps below to create your own version of Discord History Tracker.

### Setup

Fork the repository and clone it to your computer (if you've never used git, you can download the [GitHub Desktop](https://desktop.github.com) client to get started quickly).

Now you can modify the source code:
* `src/tracker/` contains JS files that are automatically combined into the **tracker bookmark/script**
* `src/viewer/` contains HTML, CSS, JS files that are then combined into the **offline viewer page**
* `lib/` contains utilities required to build the project
* `web/` contains source code of the [official website](https://dht.chylex.com), which can be used as a template when making your own website

### Building

After you've done changes to the source code, you will need to build it. Before that, download and install:
* (**required**) [Python 3](https://www.python.org/downloads)
  * Use to run the build script
* (optional) [Node + npm](https://nodejs.org/en) & command line [uglify-es](https://www.npmjs.com/package/uglify-es)
  * Not required on Windows
  * Only required for optional [JS minification](#Minification) on Linux/Mac

Now open the folder that contains `build.py` in a command line, and run `python build.py` to create a build with default settings. The following files will be created:
* `bld/track.js` is the raw tracker script that can be pasted into a browser console
* `bld/track.html` is the tracker script but sanitized for inclusion in HTML (see `web/index.php` for examples)
* `bld/viewer.html` is the complete offline viewer

You can tweak the build process using the following flags:
* `python build.py --nominify` to disable [minification](#Minification)

### Minification

The build process automatically minifies JS using `UglifyJS@3`, and CSS using a custom minifier.

* If the `--nominify` flag is used, minification will be completely disabled
* If `uglify-es` is not available from the command line, JS minification will be skipped
  * When building on Windows 64-bit, the build script will use the included Node runner and packages
  * When building on Windows 32-bit, you will need to download [Node 32-bit](https://nodejs.org/en/download) and replace the included one in `lib/`
  * When building on Linux/Mac, the build script will attempt to find `uglifyjs` in the command line, however you need to make sure it's the correct package (only `uglify-es` will work; if you install the older package just named `uglifyjs`, then it will crash and probably set something on fire)
