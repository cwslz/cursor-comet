# Cursor Comet (Mouse Trail)

[中文](README.md) | English

A lightweight Windows cursor trail effect: a transparent overlay follows the mouse and draws a glowing trail — **clicks, drags and typing pass straight through**.

[![build](https://github.com/cwslz/cursor-comet/actions/workflows/build.yml/badge.svg)](https://github.com/cwslz/cursor-comet/actions/workflows/build.yml)
[![release](https://img.shields.io/github/v/release/cwslz/cursor-comet)](https://github.com/cwslz/cursor-comet/releases/latest)
[![license](https://img.shields.io/github/license/cwslz/cursor-comet)](LICENSE)

![Preview](preview.png)

## Download / Quick start

### Installer (recommended)

Download `CursorComet-Setup.exe` from [Releases](https://github.com/cwslz/cursor-comet/releases/latest) and run it.

- Installs to `%LOCALAPPDATA%\Programs\CursorComet`; no administrator rights needed;
- Classic Everything-style wizard: language → license → install location (with space info) → progress → finish; fixed 576×302 window;
- The finish page can launch Cursor Comet; click “Finish” to close;
- Creates a Start menu shortcut (opens the settings window); optional desktop shortcut;
- Installs a standalone `uninstall.exe`, also listed in Settings → Apps;
- The installer already contains the app; no separate exe download.

The setup only installs. To remove the app, use Settings → Apps, or run `uninstall.exe` from the install folder.

### Portable

Download `cursor-comet.exe` and run it. It does not write an install directory; settings still live in `%APPDATA%\MouseTrail`.

Requires Windows 10 / 11 with .NET Framework 4.8 (built in). The exe is unsigned, so SmartScreen may warn — click “More info → Run anyway”. You can also clone the repo and run `build.ps1`.

The app has no main window; it appears in the notification area. Move the mouse to see the trail.

| Action | Description |
| --- | --- |
| `Ctrl + Alt + T` | Enable / pause the effect |
| Left / right click the tray icon | Open the settings menu |
| Tray menu → Exit | Quit the app |

The tray menu can change:

- **Style**: Comet / Rainbow / Sparks / Minimal
- **Color**: Ice Blue / Violet / Sakura Pink / Gold / Mint / White
- **Thickness**: Fine / Standard / Bold
- **Duration**: Short (0.45 s) / Medium (0.7 s) / Long (1 s)
- **Language**: Auto / 中文 / English
- **Start with Windows**
- **Reset to defaults**

## Features

- **Material You settings window**: open it from the tray menu or by double-clicking the tray icon; sidebar sections on the left, cards on the right, freely resizable
- **Bilingual UI**: switch language in the tray menu, installer and uninstaller
- Portable build is a single exe (~100 KB); the installer is ~190 KB and contains the app plus a standalone `uninstall.exe`
- No Python, Node or .NET SDK required; uses the built-in .NET Framework 4.8
- Per-pixel transparent + click-through (`WS_EX_LAYERED | WS_EX_TRANSPARENT`); normal clicking, dragging and typing are unaffected
- Full-screen transparent overlay, trail fades by **fixed time**: fast movement makes it longer instead of clipping or breaking
- High-DPI aware: thickness follows the system DPI
- Settings are saved and restored

## Settings window

Open **Open settings** from the tray menu (or double-click the tray icon) to get the Material You settings window:

- Left sidebar: Appearance, Color, Behavior, Language; About and GitHub at the bottom; thickness and duration live in Appearance → Thickness and duration;
- Right side: card-based settings; the Appearance page can toggle the effect, switch between four styles and tune style parameters;
- In rainbow mode the Color page only shows guidance; rainbow settings live in Appearance → Style parameters;
- Frameless window: drag from the brand area or the header, minimize/maximize/close at the top-right, resize from the edges;
- The window is freely resizable; on narrow widths the sidebar collapses to icons and the content scrolls;
- The settings window and the tray menu stay in sync, and language changes apply immediately.

## Style parameters

Settings → Appearance → **Style parameters** adjusts the effects of the current style:

| Style | Parameters |
| --- | --- |
| Comet | Glow |
| Rainbow | Speed, saturation, hue offset |
| Sparks | Rate, size, life, speed |
| Minimal | Width, opacity |

In rainbow mode, the Color page switches to a Rainbow settings page; the color palette is not used, and the tray color menu is disabled.

## Language

- **In the app**: tray menu → Language → Auto / 中文 / English. Applies immediately and is saved.
- **Installer / uninstaller**: language dropdown in the UI. The installer writes the chosen language to the app config.
- **Config file**: `lang=auto|zh|en`; `auto` follows the system language.

## Configuration

Location: `%APPDATA%\MouseTrail\config.ini`

```ini
# Style: comet / rainbow / sparks / minimal
style=comet

# Color: cyan / violet / pink / gold / mint / white
color=cyan

# 0.5 ~ 2
scale=1

# Duration in seconds, 0.2 ~ 1.5
life=0.7

enabled=1
showtip=0

# Language: auto / zh / en
lang=auto
```

Edit the file and restart the app. The legacy value `color=冰蓝` is migrated to `cyan` automatically. Errors are written to `error.log` in the same folder.

## Build

Requires the built-in .NET Framework 4.x (`csc.exe`).

```powershell
.\build.ps1
```

The script produces:

- `dist\鼠标拖尾.exe` (Chinese file name)
- `dist\cursor-comet.exe` (ASCII name used for releases)
- `dist\uninstall.exe` (standalone uninstaller, embedded by the setup and not published separately)
- `dist\CursorComet-Setup.exe` (installer embedding the app and the uninstaller)

It embeds `assets\app.ico` and `assets\app.manifest`. To regenerate the multi-size icon:

```powershell
.\tools\make-icon.ps1
```

## Project layout

```
cursor-comet/
├─ dist/                   # local build output, not committed
│  ├─ 鼠标拖尾.exe
│  ├─ cursor-comet.exe
│  ├─ uninstall.exe
│  └─ CursorComet-Setup.exe
├─ src/                    # C# sources
│  ├─ Program.cs           # entry point, single instance, DPI
│  ├─ TrailForm.cs         # overlay window, tray, hotkey, menu
│  ├─ TrailRenderer.cs     # four trail styles
│  ├─ TrailEngine.cs       # sampling, spark particles
│  ├─ LayeredSurface.cs    # UpdateLayeredWindow surface
│  ├─ AppSettings.cs       # settings
│  ├─ Palette.cs           # colors / HSV
│  ├─ L10n.cs              # Chinese/English strings
│  ├─ MainForm.cs          # Material You settings window
│  ├─ MaterialUI.cs        # Material controls and palette
│  ├─ IconFactory.cs       # runtime icon
│  ├─ AutoStart.cs         # Windows startup
│  └─ Native.cs            # Win32 declarations
├─ installer/
│  ├─ Common.cs            # shared setup/uninstaller code
│  ├─ Setup.cs             # installer (embeds app + uninstaller)
│  └─ Uninstaller.cs       # standalone uninstaller
├─ assets/
│  ├─ app.ico
│  └─ app.manifest
├─ .github/workflows/build.yml   # build and release on tags
├─ tools/make-icon.ps1
├─ build.ps1
├─ preview.png
├─ README.md
├─ CHANGELOG.md
├─ LICENSE
└─ README.en.md
```

## How it works

1. A borderless, topmost, full-screen window with `WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE`.
2. A 32-bpp premultiplied-alpha DIB created with `CreateDIBSection`; GDI+ draws into it and `UpdateLayeredWindow` presents it.
3. Every sample carries a timestamp; brightness and radius come from its age. Faster movement keeps a longer trail; after the mouse stops the whole trail fades out within the configured duration.
4. Smoothing uses Chaikin corner cutting, so the curve stays inside the control polygon and never overshoots into a loop.
5. The path is resampled by arc length and drawn as round-capped line segments; width depends only on sample age, so speed never changes thickness.
6. A radial gradient head glow is drawn under the trail; everything fades together, then the window is cleared to fully transparent while idle.

## FAQ

**The trail disappears?**
Check that the tray icon still exists and “Enable cursor trail” is checked; press `Ctrl + Alt + T`; exclusive full-screen games may cover the overlay — use borderless windowed mode.

**How do I disable auto-start?**
Uncheck “Start with Windows” in the tray menu, or delete `MouseTrail` under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

**How do I switch language?**
Tray menu → Language; the installer and uninstaller also have a language dropdown. Changes in the app apply immediately and are saved.

**How do I uninstall the installed version?**
Settings → Apps → Installed apps → Cursor Comet, or run `%LOCALAPPDATA%\Programs\CursorComet\uninstall.exe`. The uninstaller removes shortcuts and the uninstall entry; settings are kept by default (there is a checkbox to delete them).

**Will antivirus flag it?**
The app has no network access and no elevation; it is a plain .NET WinForms program. It is unsigned, so some scanners may warn. You can build it yourself with `build.ps1`.

**Multiple monitors?**
Thickness follows the system DPI of the primary monitor; across monitors with different scaling the size may differ slightly, but the position always follows the cursor.

**Can I change the color or curve?**
Edit the colors in `src\Palette.cs`, or the constants at the top of `src\TrailRenderer.cs`, then run `build.ps1`.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE) (GPL-3.0).

Copyright (C) 2026 MouseTrail contributors

You may use, modify and distribute it freely; if you distribute a modified version, you must provide the source under GPL-3.0 as well.
