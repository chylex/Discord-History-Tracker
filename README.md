# Usage

Visit the [official website](https://dht.chylex.com) to add Discord History Tracker (beta) to your bookmarks and download the renderer.

# Building

The build script requires **Python 3**. For automatic build, run `python build.py`, and a `bld` folder with the track script and renderer will be created.

The `track.js` script is ready to be added as a bookmark in a browser, or ran in a browser console.

## Minification

The build process has support for JS and CSS minification.

If possible, it uses **YUI Compressor** for CSS, and **UglifyJS** for JavaScript (falls back to **Google Closure Compiler** if **UglifyJS** is not available). If the required programs are not found on the system path, minification will be disabled without warnings.

It is possible to disable minification completely using the `--nominify` flag, or to force **Google Closure Compiler** to be used using the `--closure` flag.

### Requirements

- **Java 7+** (YUI, Closure Compiler)
- **Node.js** (UglifyJS)
- **uglify-js-harmony** (UglifyJS)

### Setting Up UglifyJS

Once you install `Node.js` which contains `npm`, use the following command to download UglifyJS with ES6 support and add it to your system path:
```
npm install uglify-js-harmony -g
```

### UglifyJS vs Google Closure Compiler

Closure Compiler compiles into ES5, which adds support for older browsers that don't have some of the used ES6 functionality, however it is at the expense of several additional kilobytes to the file size.
