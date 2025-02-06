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
* `web/` contains source code of the [official website](https://dht.chylex.com), which can be used as a template when making your own website

To start editing source code for the desktop app, install the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0), and then open `app/DiscordHistoryTracker.sln` in [Visual Studio](https://visualstudio.microsoft.com/downloads/) or [Rider](https://www.jetbrains.com/rider/).

### Building

To build a `Debug` version of the desktop app, there are no additional requirements.

To build a `Release` version of the desktop app, follow the instructions for your operating system.

#### Release – Windows (64-bit)

1. Install [Powershell 5](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows) or newer (on Windows 10, the included version of Powershell should be enough)

Run the `app/build.bat` script, and read the [Distribution](#distribution) section below.

#### Release – Other Operating Systems

1. Install the `zip` package from your repository

Run the `app/build.sh` script, and read the [Distribution](#distribution) section below.

#### Distribution

The mentioned build scripts will prepare `Release` builds ready for distribution. Once the script finishes, the `app/bin` folder will contain self-contained executables for each major operating system, and a portable version that works on all other systems but requires the ASP.NET Core Runtime to be installed.

Note that when building on Windows, the generated `.zip` files for Linux and Mac will not have correct file permissions, so it will not be possible to run them by double-clicking the executable. Since .NET 8 fixed several issues with publishing Windows executables on Linux, I recommend using Linux to build the app for all operating systems.
