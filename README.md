# Usage

Visit the [official website](https://dht.chylex.com) to add Discord History Tracker (beta) to your bookmarks and download the renderer.

# Building

The build script requires **Python 3**. For automatic build, run `python build.py`, and a `bld` folder with the track script and renderer will be created.

The `track.js` script is ready to be added as a bookmark in a browser, or ran in a browser console. The `track.html` contains a bookmarkable link you can easily include on a website.

## Minification

The build process automatically minifies the generated files. **YUI Compressor** is used for CSS and **UglifyJS** is used for JavaScript. To disable minification, use the `--nominify` flag.

**YUI** requires **Java 7+** on the PATH. If Java is not available, CSS compression will be skipped.

**UglifyJS** is executed using the included **Node** runner with all packages already installed in the repository.
