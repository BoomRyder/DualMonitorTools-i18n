# Dual Monitor Tools v2.13

Dual Monitor Tools is a collection of tools for users running multiple monitor setups
on Windows.

Author: Gerald Evans. Licensed under the GNU General Public License v3 — see
[`COPYING.txt`](COPYING.txt) for the full license text.

For further information on usage of the tools, visit <http://dualmonitortool.sourceforge.net>

## Contents

- [Tools](#tools)
- [Requirements](#requirements)
- [Installation](#installation)
- [Hotkeys](#hotkeys)
- [Magic Words](#magic-words)
- [Portable Usage](#portable-usage)
- [Data Files](#data-files)
- [Uninstall](#uninstall)
- [Known Problems](#known-problems)
- [Building from Source](#building-from-source)
- [Credits](#credits)
- [Change History](#change-history)

## Tools

Currently the tools available are `DMT.exe`, `DualWallpaper.exe` and `DmtWallpaper.scr`.

### DMT.exe

The main tool, which resides in the Windows Notification Area. It contains the
following modules:

- **Cursor** — Control movement of cursor between monitors. The cursor can be
  locked to a monitor or made sticky, so extra effort is required to move it
  between monitors.

- **Launcher** — Allows you to launch your favourite applications with a few key
  strokes and to position them at pre-configured positions on any of your
  monitors. All other DMT commands can also be run this way.

- **Snap** — Take snapshot of primary monitor and show on secondary. Previous
  snapshots can be viewed, saved to a file or copied to the clipboard.

- **Swap Screen** — Allows easy movement of windows between monitors, and allows
  Windows to be moved to pre-defined positions.

- **Wallpaper Changer** — Changes wallpaper periodically using a variety of
  sources and allows you to control how images are shown across your monitors.

### DualWallpaper.exe

Allows the manual creation of wallpaper for multiple monitors.

### DmtWallpaper.scr

Works with DMT.exe to show your wallpaper when the screen saver is running.

## Requirements

Dual Monitor Tools should run on any version of Windows that has .NET 4.0 or later
installed.

Ideally you need two or more monitors attached to the computer, but some of the
features can be useful to users with a single monitor.

## Installation

There are 2 choices for installation:

1. Use the MSI installer — this is the simplest method and will make installing
   any future versions even easier.
2. Portable install from a zip file — this gives you the most flexibility and is
   the route to use if you are installing to a portable device.

### MSI installation for new users

Just download `DualMonitorTools-2.13.msi` and run it.

### MSI installation upgrades

If you have previously installed DualMonitorTools using the MSI installer then you
can just download `DualMonitorTools-2.13.msi` and run it. Alternatively, you can do
this via the options in Dual Monitor Tools, by clicking the "Check for Updates"
button in the "Dual Monitor Tools" → General page.

### MSI installation for existing install done from a zip file

With an MSI installation, the executable and configuration files will live in
different locations, so there are some extra steps that you need to go through if
you wish to keep your existing configuration:

1. In your existing version of DMT, make sure "Start when Windows starts" (found in
   Options → Dual Monitor Tools → General) is not ticked.
2. Make sure DMT is not currently running.
3. Make sure you have a backup copy of the following configuration files:
   - `DmtMagicWords.xml`
   - `DmtSettings.xml`
   - `DmtWallpaperProviders.xml`

   These would normally be in the same directory that you previously installed
   Dual Monitor Tools to.
4. Download and run the new MSI installer.
5. Double click on DMT in the notification area.
6. Select the 'General' page under 'Dual Monitor Tools'. This will show the location
   where the configuration files now live. The files will be in the
   `%appdata%\Dual Monitor Tools` folder, which should be something like:
   `C:\Users\<your login name>\AppData\Roaming\Dual Monitor Tools`
7. Stop DMT.
8. Copy the 3 xml files into the above location.
9. Start DMT. There should be an option in the Start menu to do this.
10. Don't forget to tick the "Start when Windows starts" option if required.

Note: future updates will be much simpler as they can be performed from within DMT.

### Portable installation

1. Download the zip file.
2. If you are already running the files from a previous version, then stop them.
3. Unzip the zip file to your desired location.
4. The tools are now ready to run. You may need to give Windows permission to run
   the files as they were downloaded from the internet.

## Hotkeys

Most of the actions are accessible via hotkeys. You will need to define hotkeys for
the actions that you want to use. Not all key combinations are available as Windows
reserves quite a few for itself, and each new version of Windows grabs more and more
of them.

Probably the most important hotkey to define is the 'Activate Magic Word input'
under Launcher → Hotkeys, as this will then allow all other actions to be actioned
by the use of this hotkey followed by a magic word. On a UK keyboard
`Win+<OemPipe>` is a good hotkey to use for this as it is easy to enter. For other
keyboard layouts, you may need to experiment to see what is available and easy to
enter.

On the same page is the 'Add Magic Word for current application' hotkey. If you
press this hotkey, while running another application, it will offer to create a
magic word which you can then use in the future as a quick way of starting that
application. `Win+A` is a nice hotkey for this, but be aware that Windows 10 now
uses this key combination.

## Magic Words

You can associate applications, websites and DMT commands with a Magic Word. When
you hit the launcher hotkey, a dialog box will pop up allowing you to start entering
the magic word. This uses auto-completion so you won't normally have to type in the
whole word.

You can also assign the same Magic Word to multiple applications. When you enter the
Magic Word, all of the applications will be started.

## Portable Usage

The tools are designed to be easy to use in a portable environment, so they can be
copied to a USB drive and ran direct from there. Make sure you install from the zip
file and not from the MSI installer.

When specifying filenames (for the launcher executables and wallpaper locations),
you may want to make sure that any paths you enter are relative paths if they exist
on the USB drive to ensure portability.

## Data Files

In a portable installation, the tools will write to data files in the same directory
that the executables are. The files written to are:

- `DmtMagicWords.xml` — Magic words used by the Launcher.
- `DmtWallpaperProviders.xml` — Specifies wallpaper image source.
- `DmtSettings.xml` — Any other settings go in here.
- There will also be `.bak` versions of these files which are just backup copies of
  the previous versions of the files.

In an MSI installation, there will be a `DmtFileLocations.xml` in this directory that
will specify where the other xml files live, which would be in
`%APPDATA%\Dual Monitor Tools`.

### Customising data file locations

You only need a `DmtFileLocations.xml` if you want the data files to reside in a
directory other than the one where `DMT.exe` lives.

This file needs to be named `DmtFileLocations.xml` and to reside in the same
directory as `DMT.exe`. It will not be written to, so is safe to reside under
`C:\Program Files (x86)`.

Normally you would just need to change the location for `datadirectory`, unless you
wanted finer control over the filenames, or wanted to enable the log file.
Environment variables may be used in the paths below, or you can use the explicit
pathnames.

```xml
<?xml version="1.0" encoding="utf-8"?>

<filelocations>
	<!-- datadirectory specifies the default home directory
	     for all file locations that are not explicitly set below.
	     This must be a full path, but may include environment variables.
	     You will need to make sure that the directory exists before running DMT. -->
	<filelocation name="datadirectory" value="%APPDATA%/DMT" />

	<!-- Individual file locations may be the full or relative path.
	     If a relative path is used, then this will be relative to datadirectory if specified,
	     otherwise it will be relative the location of DMT.exe -->

	<!-- Location for generic settings -->
	<!-- <filelocation name="settings" value="DmtSettings.xml" /> -->

	<!-- Location for saved magic words -->
	<!-- <filelocation name="magicwords" value="DmtMagicWords.xml" /> -->

	<!-- Location for saved wallpaper providers -->
	<!-- <filelocation name="wallpaperproviders" value="DmtWallpaperProviders.xml" /> -->

	<!-- Location for image to be used when creating Windows wallpaper -->
	<!-- <filelocation name="wallpaper" value="DmtWallpaper.bmp" /> -->

	<!-- Location for log file (for debugging).
	     If not specified, then no log file will be used. -->
	<!-- <filelocation name="log" value="Dmt.log" /> -->
</filelocations>
```

Save the above as `DmtFileLocations.xml` (UTF-8 with BOM) next to `DMT.exe`.

## Uninstall

### Uninstall of MSI installation

You can uninstall from within the Windows installed application list. Any saved
settings will need to be removed manually by deleting the folder
`%APPDATA%\Dual Monitor Tools`.

### Uninstall of portable installation

If either 'Start when Windows starts' or 'Add to desktop context menu' have been
enabled on DMT → Options → 'Dual Monitor Tools' → General, then you will need to
uncheck these before removing anything.

Then make sure none of the tools are running, and you can just delete the files you
unzipped and the data files mentioned above.

## Known Problems

If you wish DMT to fully work when you have windows from applications that have been
'Run as Administrator', then you will also need to start DMT with
'Run as Administrator'.

## Building from Source

This repository contains the source code. To build all three solutions:

```powershell
cd <repo root>
.\build.ps1                                 # Debug build
.\build.ps1 -Configuration Release          # Release build
.\build.ps1 -Configuration Release -Test    # Build + run 148 NUnit tests
.\build.ps1 -Clean                          # Clean bin/obj
```

Requirements: Visual Studio 2019/2022 with the ".NET desktop development" workload
(includes .NET Framework 4.0 Client Profile and 4.5 targeting packs plus MSBuild),
and network access to nuget.org (or pre-provisioned NUnit packages under
`DMT\packages`).

Outputs: `DMT\DMT\bin\<Cfg>\DMT.exe`,
`DualWallpaper\DualWallpaper\bin\<Cfg>\DualWallpaper.exe`,
`DmtWallpaper\DmtWallpaper\bin\<Cfg>\DmtWallpaper.exe` and `.scr`.

The untouched upstream 2.13 source archive is preserved as git tag `upstream-2.13`;
compare against it to see every local modification:

```
https://github.com/BoomRyder/Dual_Monitor_Tools_cn/compare/upstream-2.13...main
```

## Credits

Special thanks to the following:

- Shawn Steele for adding the background colour selector in DualWallpaper.
- Nithin Philips for adding the drag and drop to DualWallpaper.
- Christopher Drexler for the command line option to DualWallpaper.
- Ryan Kirby for identifying bug in previous screen hotkey.
- Mario D'Amico for adding the Bing wallpaper downloader.

And all those that have contributed to the forum with bugs and suggestions for
improvements.

## Change History

### Changes from 2.12 to 2.13

- DMT:SwapScreen Prevents user from accidentally moving start menu and task bar
- DMT:General Monitors table now shows the GPU that the monitor is attached to
- DMT:General Added Top-down, left-right monitor ordering
- DMT:General Users can now give their own name to each physical monitor
- DMT:General Can order monitors by name
- DMT:Cursor Added hotkey to lock cursor to current monitor
- DMT:Cursor Primary monitor group can now be specified using monitor names

### Changes from 2.11 to 2.12

- DMT:SwapScreen Now ignores windows that are not on the current virtual desktop
- DMT:SwapScreen Added hotkey to move all windows to a specified screen
- DMT:SwapScreen 'Move to next screen' logic updated to better handle different display sizes
- DMT:General Monitor orientation can be changed where supported by monitor
- DMT: Many of the command line commands that take a display number can now take 'X' for the display that the cursor is on
- DMT: Monitor name now displayed (if room) on the monitor order and working area screens
- DMT: Added (optional) desktop context menu for some of the most used commands — option is in DMT → Options → 'Dual Monitor Tools' → General

### Changes from 2.10 to 2.11

- DMT:Cursor Fix for issue where stickiness stops working
- DMT:Wallpaper New option to stop wallpaper changing when gaming or doing presentations
- DMT:Snap Fix for snaps shown on wrong monitor in certain circumstances
- DMT: Better handling of monitor resolution changes
- DMT: F13 to F24 can now be used as hotkeys

### Changes from 2.9 to 2.10

- DMT:Wallpaper User can select wallpaper to be saved as JPEG, PNG or BMP
- DMT:Wallpaper Any image files that can't be loaded are now logged
- DMT:SwapScreen On initial install, the UDAs are no longer automatically added as magic words
- DMT:Cursor Fix for cases where some display resolution changes were not noticed

### Changes from 2.8.1 to 2.9

- DMT:Cursor can now specify a primary group of monitors using a GUI
- DMT:General monitor brightness/contrast can now be adjusted with delta values
- DMT:Wallpaper new provider that takes a list of image files/URLs

### Changes from 2.8 to 2.8.1

Temporary release (all changes will be absorbed into 2.9)

- DMT:General Monitors grid fix for sizes/areas of rotated monitors
- DMT:General Monitors grid, can now view / adjust contrast (if supported by monitor)
- DMT:Cursor Introduced the concept of a group of primary monitors

When a group of primary monitors is specified, the "Allow cursor to return freely to
the primary screen" can now be read as "Allow cursor to return freely to any monitor
in the primary group". If a (valid) group of primary monitors is not specified, then
the original functionality will be retained.

A GUI will be added for 2.9 to specify which monitors belong to the primary group.
For now, it needs to be done by manually editing the `DmtSettings.xml` file. The
location of this file can be determined from Options → "Dual Monitor Tools" →
General → Settings.

Make sure DMT is not running when you edit this file. If you have 4 monitors, which
would be 1, 2, 3, 4 and you want to make 2 and 3 act as the primary group, then add
a line like:

```xml
<setting name="Cursor__PrimaryGroup" value="2,3" />
```

within the `<settings> ... </settings>` block. (Note there are two underscore
characters between "Cursor" and "PrimaryGroup".)

If you then start DMT and have sticky/locked cursor in force and the "Allow cursor
to return freely to the primary screen" checked, then you will be able to move the
cursor freely between screens 2 and 3 and onto these screens from the other screens,
but the sticky/locked operation will apply if you try to move the cursor onto
screens 1 and 4.

### Changes from 2.7 to 2.8

DMT New monitor numbering/ordering.

The default numbering of monitors has changed in 2.8 so that monitors are now
numbered left to right and then top to bottom instead of the old ordering provided
by the .NET API. If this is an issue for you, then you can go into
Options → 'Dual Monitor Tools' → 'Monitor Order' and select 'DotNet order, as used
by DMT v2.7 and earlier'.

- DMT:Wallpaper added support for wallpapers from Bing
- DMT:Wallpaper can show images from local disk in alphabetical order
- DMT:Wallpaper handles failure to download images better
- DMT:Cursor allows cursor to move via dead space for non rectangular layouts
- DMT:Snap can specify monitor to put snap on

### Changes from 2.6 to 2.7

- DMT:Wallpaper changer image fill method is more configurable
- DMT:Wallpaper changer properties show a few more details
- DMT:Wallpaper changer updated for change to Unsplash website
- DMT:SwapScreen SDA option to put (transparent) border on outside of SDA

### Changes from 2.5 to 2.6

- DMT:SwapScreen now allows up to 100 user defined areas
- DMT:SwapScreen added system defined areas for each screen/half/quarter
- DMT:SwapScreen fix for snap up/down/left/right when taskbar at (0, 0)
- DMT:Wallpaper changer can now enable/disable providers from grid
- DMT:Cursor fix for cursor sometimes passing to another screen when it shouldn't

### Changes from 2.4 to 2.5

- DMT:Wallpaper changer fixed flicker when using smooth changing
- DMT:SwapScreen added a parametised "ShowDesktop" command
- DMT:Wallpaper changer added a new URL image provider
- DMT:Wallpaper changer Unsplash image provider has more configuration
- DMT:Wallpaper changer new option to cycle though all local disk images before repeating
- DMT:SwapScreen new hotkey to show desktop that cursor is on
- DMT:SwapScreen option to reset the UDAs
- DMT:SwapScreen "ShowDesktop" now supports up to 16 monitors
- DmtWallpaper screen saver added

### Changes from 2.3 to 2.4

- DMT:General now shows information about monitors
- DMT:General can now change active monitor
- DMT:General can now change monitor brightness
- DMT can now be closed from the command line with `-x` option
- DMT:Cursor fix for remembering free movement key
- DMT:Snap fix for snaps that are smaller then secondary on one dimension only
- DMT:SwapScreen fix for 'Next Screen' when taskbar at left or top of monitor
- DMT:Wallpaper changer obeys exif orientation flag
- DMT:Wallpaper changer temporary fix for Unsplash images

### Changes from 2.2 to 2.3

- Installer now available.
- DMT:SwapScreen fix for when swapping top 2 windows
- DMT:WallpaperChanger makes sure correct wallpaper mode used if using smooth changes
- DMT:Cursor fix for cursor not sticky after Ctrl-Alt-Del
- DMT No longer allows multiple instances of its GUI interface to be run

### Changes from 2.1 to 2.2

- Magic words can be run from a batch file with: `DMT "DMT:Launcher:Activate magic-word"`
- DMT:Snap hotkey added to snap active window
- DMT:Snap snap shown same size (pixel basis) as original with options to scale
- DMT:WallpaperChanger LocalDisk can specify different directories for each monitor
- DMT:WallpaperChanger LocalDisk can specify a single image rather than a directory

### Changes from 2.0 to 2.1

- Now just needs the client profile subset of the .NET 4 framework, rather than the full version
- DMT Actions can now be performed from the command line / batch files
- DMT File locations are shown in options for troubleshooting
- DMT:Cursor Option to allow mouse buttons to be used to cross cursor boundaries
- DMT:Cursor Added hotkey to move cursor to center of primary monitor
- DMT:SwapScreen fixed hotkey to move active window to previous monitor
- DMT:WallpaperChanger has crossfade option for changing wallpaper
- DMT:WallpaperChanger shows details of images used in wallpaper
- DMT:WallpaperChanger Flickr added as an image provider
- DMT:WallpaperChanger Can choose different local disk folder for monitors in portrait orientation
- DMT:WallpaperChanger Unsplash provider updated for changes to their website

### Changes from 1.10 to 2.0

New tool DMT provides the functionality previously provided by SwapScreen,
DualLauncher, DualSnap and DualWallpaperChanger together with extra functionality.

DMT now easier to use from a portable device. All features within DMT can be
launched with 'Magic Words'. DisMon has been dropped for the time being.

### Changes from 1.9 to 1.10

Functional:

- Added new tool Dual Wallpaper Changer
- DualWallpaper has limited support for being run from the command line or batch scripts
- SwapScreen has a choice for key to use to allow cursor to move between screens

Bug fixes:

- SwapScreen should now work better with touch screens
- DualWallpaper interpolation mode changed to improve image quality

### Changes from 1.8 to 1.9

Functional:

- DualWallpaper now supports drag and drop (thanks to Nithin Philips for this)
- SwapScreen has new hotkey to allow the top two windows to be be swapped
- SwapScreen has 4 new hotkeys for snap left/right/up/down

Windows 8 support:

- DualWallpaper works with win 8 when primary monitor not left/top most

Bug fixes:

- SwapScreen has improved handling of configuration saving problems
- DualWallpaper supports DPI scaling other than 100%
- DualWallpaper now also supports .jpeg (with an e) extension for input images

### Changes from 1.7 to 1.8

Functional:

- SwapScreen now allows you to move the active window to any of 10 user defined areas
- SwapScreen now has an option to allow sticky/locked cursors to return freely to the primary screen
- The background colour can now be set in DualWallpaper
- Option `-n` added to DisMon to stop it from restoring the monitors to their original state before it exits
- Option `-x` added to DisMon to tell it to change monitor states and exit

Bug fixes:

- Cancelling the 'Change hotkey' dialog did not always leave the hotkey unchanged
- In DualLauncher, the disable hotkey operation did not disable the hotkey

Other:

- Changes to SwapScreen to reduce memory allocations

### Changes from 1.6 to 1.7

Functional:

- DualSnap now has hotkey to toggle display of snap.
- SwapScreen can set default cursor mode at startup.
- DualSnap, DualLauncher and SwapScreen now allow more keys to be used as hotkeys.

Bug fixes:

- SwapScreen cursor locking/stickiness had problems with some layouts with screens with different resolutions.
- DualWallpaper would crash if it couldn't write to a wallpaper.
- DualLauncher could sometimes forget its default screen position.
- SwapScreen fix to hooking which could cause problems if other programs also hook the mouse and/or keyboard.

Other:

- DualSnap options window re-arranged so hotkey editing is in same style as the other tools.

### Changes from 1.5 to 1.6

Functional:

- Added new tool DualLauncher.

Bug fixes:

- SwapScreen handles supersized windows correctly when taskbar stretches across entire desktop.

Other:

- DualWallpaper interface cleaned up to fit on smaller screens.

### Changes from 1.4 to 1.5

Functional:

- SwapScreen's 'Minimise Active Window' now works as a toggle.
- SwapScreen now has a portable mode where its settings can live in the same directory as the executable.
- DisMon can now also change the primary monitor.
- Command line interface to DisMon changed for better flexibility.

Other:

- DisMon7 functionality (for Windows 7) now incorporated into DisMon.

### Changes from 1.3 to 1.4

Functional:

- Added SwapScreen can now restrict cursor movement between screens.

Other:

- New icons and about box images used.

### Changes from 1.2 to 1.3

Functional:

- Added new tool DisMon and DisMon7.
- Added 'start when Windows starts' to SwapScreen and DualSnap.
- Added zoom and center fit to DualWallpaper.

### Changes from 1.1 to 1.2

Functional:

- Added new tool DualWallpaper.
- Added 9 new hotkeys to SwapScreen.

Bug fixes:

- Fixed menu scrolling problem in DualSnap.

### Changes from 1.0 to 1.1

Functional:

- Added new tool DualSnap.

Other:

- Added link from SwapScreen's context menu to website.
