# Dual Monitor Tools

**Windows tools for multi-monitor cursors, windows, launchers and wallpaper.**

**English** | [简体中文](./docs/README.zh-CN.md) | [繁體中文](./docs/README.zh-Hant.md) | [日本語](./docs/README.ja.md) | [한국어](./docs/README.ko.md)

[![license](https://img.shields.io/badge/license-GPL--3.0-blue)](./COPYING.txt)
[![platform](https://img.shields.io/badge/platform-Windows-blue)](http://dualmonitortool.sourceforge.net)

[Features](#features) • [Quick start](#quick-start) • [Build](#build) • [Changelog](#changelog) • [License](#license)

---

Dual Monitor Tools is a Windows toolkit for people who use more than one monitor. This repository is a localized fork of Gerald Evans' 2.13 release. The interface can be Simplified Chinese, Traditional Chinese, Japanese, Korean or English. A new install starts in Simplified Chinese.

Upstream project: <http://dualmonitortool.sourceforge.net>

## Features

- 🖱️ **Cursor** — lock the pointer to one monitor, or make it sticky so it takes extra effort to cross to the next screen
- 🚀 **Launcher** — start programs, documents and web pages with a short magic word, and place their windows where you want
- 📸 **Snap** — copy the primary monitor onto another screen, then view, save or copy earlier snapshots
- ↔️ **Swap Screen** — move windows between monitors, or snap them to halves, quarters and saved areas
- 🖼️ **Wallpaper Changer** — rotate wallpaper from several sources and control how one image covers many monitors
- 🧩 **DualWallpaper** — build a multi-monitor wallpaper by hand
- 💤 **DmtWallpaper** — a screen saver that shows the wallpaper DMT last created
- 🌐 **Five interface languages** — Simplified Chinese, Traditional Chinese, Japanese, Korean and English. Change it under *Options → Dual Monitor Tools → General → Language*; the choice is remembered

## Quick start

| Form | What you run | Notes |
| --- | --- | --- |
| **Setup** | `installer\DualMonitorTools.msi` | Needs administrator rights. Data goes to `%APPDATA%\Dual Monitor Tools` |
| **Portable** | `DMT.exe` from a folder you choose | Data stays next to the executable. Do not ship `DmtFileLocations.xml` with it |
| **From source** | `.\build.ps1`, then `installer\Build.bat` | See [Build](#build) |

The MSI is the upstream WiX package. It writes `installed=1`, installs the screen saver through the 32-bit system folder (on 64-bit Windows that is `SysWOW64`), and still creates the empty desktop context-menu key. It does not include the language satellite assemblies; without those DLLs the interface falls back to English.

**Check for Updates** asks before it opens or installs an upstream release. The default answer is No. Choosing Yes replaces this build with the original English release.

## Build

Requirements: Visual Studio 2019 or 2022 with the .NET desktop development workload (.NET Framework 4.0 Client Profile and 4.5), and WiX Toolset 3.14 for the MSI.

```powershell
.\build.ps1                              # Debug
.\build.ps1 -Configuration Release -Test # Release and tests
cd installer
.\Build.bat                              # MSI, after a Release build
```

Outputs:

- `DMT\DMT\bin\<Cfg>\DMT.exe`
- `DualWallpaper\DualWallpaper\bin\<Cfg>\DualWallpaper.exe`
- `DmtWallpaper\DmtWallpaper\bin\<Cfg>\DmtWallpaper.exe` and `.scr`
- `installer\DualMonitorTools.msi`

Language satellites are built beside each program, in `zh-Hans`, `zh-Hant`, `ja` and `ko` folders.

## Changelog

Changes in this fork: the five-language interface, and a confirmation before Check for Updates follows an upstream download. The installer is the author's WiX project again.

Gerald Evans' history from 1.0 through 2.13 is in [docs/CHANGELOG.md](./docs/CHANGELOG.md). The short notes shipped inside the MSI remain `CHANGES.txt`.

## License

[GNU General Public License v3](./COPYING.txt). Copyright (C) Gerald Evans for the original program. You may use, modify and distribute this program under that license. It comes with no warranty.
