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

To start editing source code for the desktop app, install the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0), and then open `app/DiscordHistoryTracker.sln` in [Visual Studio](https://visualstudio.microsoft.com/downloads/) or [Rider](https://www.jetbrains.com/rider/).

### Building

To build a `Debug` version of the desktop app, there are no additional requirements.

To build a `Release` version of the desktop app, follow the instructions for your operating system.

#### Release – Windows (64-bit)

1. Install [Python 3](https://www.python.org/downloads), and ensure the `python` executable is in your `PATH`
2. Install [Powershell 5](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows) or newer (on Windows 10, the included version of Powershell should be enough)

The `lib/` folder contains an installation of [Node](https://nodejs.org/en) and [uglify-js](https://www.npmjs.com/package/uglify-js), which are used to minify the tracking script. This installation will only work on 64-bit Windows; building on 32-bit Windows is not supported, but you can try.

Run the `app/build.bat` script, and read the [Distribution](#distribution) section below.

#### Release – Other Operating Systems

1. Install [Python 3](https://www.python.org/downloads), and ensure the `python` executable exists and launches Python 3
   - On Debian and derivatives, you can install `python-is-python3`
   - On other distributions, you can create a link manually, for ex. `ln -s /usr/bin/python3 /usr/bin/python`
   - If you don't want `python` to mean Python 3, then edit `Desktop.csproj` and change `python` to `python3`
2. Install [Node + npm](https://nodejs.org/en)
3. Install [uglify-js](https://www.npmjs.com/package/uglify-js) globally (`npm install -g uglify-js`)
4. Install the `zip` package from your repository

Run the `app/build.sh` script, and read the [Distribution](#distribution) section below.

#### Distribution

The mentioned build scripts will prepare `Release` builds ready for distribution. Once the script finishes, the `app/bin` folder will contain self-contained executables for each major operating system, and a portable version that works on all other systems but requires .NET 8 to be installed.

Note that when building on Windows, the generated `.zip` files for Linux and Mac will not have correct file permissions, so it will not be possible to run them by double-clicking `DiscordHistoryTracker`. I tried using Python to re-create the archives with correct file permissions, but found that Linux `zip` tools could not see them. The only working solution is building the Windows + portable version on Windows, and Linux + Mac version on Linux.
