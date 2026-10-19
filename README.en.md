# Cursor Comet (Mouse Trail)

[中文](README.md) | English

A lightweight Windows cursor trail effect: a transparent overlay follows the mouse and draws a glowing trail — **clicks, drags and typing pass straight through**.

> **Preview**: 1.x is a development preview; features and config format may change. It is not recommended as your only daily-use version before 2.0. When reporting issues, include your Windows version, display scaling, and `%APPDATA%\CursorComet\error.log`.

[![build](https://github.com/cwslz/cursor-comet/actions/workflows/build.yml/badge.svg)](https://github.com/cwslz/cursor-comet/actions/workflows/build.yml)
[![release](https://img.shields.io/github/v/release/cwslz/cursor-comet)](https://github.com/cwslz/cursor-comet/releases/latest)
[![license](https://img.shields.io/github/license/cwslz/cursor-comet)](LICENSE)

![Preview](preview.png)

![Fluent settings window](docs/screenshot-appearance.png)

![Installer](docs/screenshot-installer.png)

## Download / Quick start

### Installer (recommended)

Download `CursorComet-Setup.exe` from [Releases](https://github.com/cwslz/cursor-comet/releases/latest) and run it.

- Installs to `%LOCALAPPDATA%\Programs\CursorComet`; no administrator rights needed;
- Standard Inno Setup 6 wizard: language (Chinese / English) → license → install location → optional tasks → progress → finish;
- The finish page can launch Cursor Comet; click “Finish” to close;
- Creates a Start menu shortcut (opens the settings window); optional desktop shortcut;
- To remove it, use Settings → Apps, or run `unins000.exe` in the install folder; the `%APPDATA%\CursorComet` settings are kept by default;
- Silent install: `CursorComet-Setup.exe /VERYSILENT /LANG=en`, optionally with `/TASKS="desktopicon,autostart"`;
- The installer already contains the app; no separate exe download.

The setup only installs. To remove the app, use Settings → Apps, or run `unins000.exe` from the install folder.

### Portable

Download `cursor-comet.exe` and run it. It does not write an install directory; settings still live in `%APPDATA%\CursorComet`.

Requires Windows 10 / 11 with .NET Framework 4.8 (built in). The exe is unsigned, so SmartScreen may warn — click “More info → Run anyway”. You can also clone the repo and run `build.ps1`.

The app has no main window; it appears in the notification area. Move the mouse to see the trail.

| Action | Description |
| --- | --- |
| `Ctrl + Alt + T` | Globally disable / enable effects |
| Left / right click the tray icon | Open the tray menu |
| Tray menu → Exit | Quit the app |

The tray menu has exactly four items:

- **Cursor Comet**: title;
- **Open main window**: opens the Fluent-style settings window;
- **Disable effects / Enable effects**: temporarily pause/resume the trail, glow and halo; the menu label switches on the next open;
- **Exit**: quit the app.

Style, color, thickness, duration, language and auto-start live in the main window.

## Features

- **Fluent Design-style settings window (light)**: open it from the tray menu or by double-clicking the tray icon; sidebar sections on the left, cards on the right, freely resizable
- **Bilingual UI**: switch language in the main window, installer and uninstaller
- Portable build is a single exe (~120 KB); the installer is a ~2.1 MB Inno Setup 6 package that contains the app and generates the uninstaller
- No Python, Node or .NET SDK required; uses the built-in .NET Framework 4.8
- Per-pixel transparent + click-through (`WS_EX_LAYERED | WS_EX_TRANSPARENT`); normal clicking, dragging and typing are unaffected
- Full-screen transparent overlay, trail fades by **fixed time**: fast movement makes it longer instead of clipping or breaking
- High-DPI aware on the primary display; mixed-DPI multi-monitor overlays use the primary display scale and may differ
- Settings are saved and restored

## Settings window

Open **Open main window** from the tray menu (or double-click the tray icon) to get the Fluent Design settings window:

- Left sidebar: Appearance, Color, Halo, Behavior, Language; About and GitHub at the bottom; thickness and duration live in Appearance → Thickness and duration;
- Right side: card-based settings; the Appearance page can toggle the effect, switch between four styles and tune style parameters;
- In rainbow mode the Color page only shows guidance; rainbow settings live in Appearance → Style parameters;
- Frameless window: drag from the brand area or the header, minimize/maximize/close at the top-right, resize from the edges;
- The window is freely resizable; on narrow widths the sidebar collapses to icons and the content scrolls;
- The settings window’s “Enable cursor trail” only controls the trail; the tray/hotkey “Disable effects” is a global pause that also suppresses glow and halo; language changes apply immediately.

## Style parameters

Settings → Appearance → **Style parameters** adjusts the effects of the current style:

| Style | Parameters |
| --- | --- |
| Comet | Glow moved to Halo |
| Rainbow | Speed, saturation, hue offset |
| Sparks | Rate, size, life, speed |
| Minimal | Width, opacity |

## Halo

Settings → **Halo**:

- **Glow**: toggle + radius / strength / color; for Comet the glow follows the trail and fades with it, while other styles show the glow on the cursor; the color can be Auto or a fixed color, independent of rainbow mode;
- **Halo**: toggle + radius / thickness / opacity / color; an independent ring around the cursor.

Each can be enabled separately, both at once, or both off. The halo does not depend on the trail style and stays on the cursor after the trail fades out; turning off **Enable cursor trail** only stops the trail, while the glow and halo stay visible.

The comet's Style parameters show a note that the glow has moved to Halo, with a button to jump there.

In rainbow mode, the Color page only shows guidance; the color palette is not used, and rainbow settings live in Appearance → Style parameters.

## Language

- **In the app**: tray menu → Language → Auto / 中文 / English. Applies immediately and is saved.
- **Installer / uninstaller**: language dropdown in the UI. The installer writes the chosen language to the app config.
- **Config file**: `lang=auto|zh|en`; `auto` follows the system language.

## Configuration

Location: `%APPDATA%\CursorComet\config.ini` (the first start migrates the old `%APPDATA%\MouseTrail\config.ini` automatically)

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

- `dist\cursor-comet.exe` (main executable)
- `dist\鼠标拖尾.exe` (Chinese file name alias)
- `dist\CursorComet-Setup.exe` (Inno Setup 6 installer embedding the app)

It embeds `assets\app.ico` and `assets\app.manifest`. Building the installer requires Inno Setup 6 (`ISCC.exe`); `build.ps1` looks in `%LOCALAPPDATA%\Programs\Inno Setup 6` and `Program Files (x86)` automatically. The build also writes `dist\SHA256SUMS.txt` with the hashes of both artifacts. Run the tests with:

```powershell
.\test.ps1
```

`build.ps1` must run first to generate `src\VersionInfo.cs`. The 27 checks cover config round-trip, clamping, palette parsing, failed atomic saves, corrupt-config fallback and legacy config migration. The single source of truth for the version is `version.txt`; `build.ps1` generates `src\VersionInfo.cs` — do not edit that generated file. To regenerate the multi-size icon:

```powershell
.\tools\make-icon.ps1
```

## Project layout

```
cursor-comet/
├─ dist/                   # local build output, not committed
│  ├─ cursor-comet.exe
│  ├─ 鼠标拖尾.exe
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
│  ├─ MainForm.cs          # Fluent Design settings window
│  ├─ FluentUI.cs          # Fluent controls and palette
│  ├─ IconFactory.cs       # runtime icon
│  ├─ AutoStart.cs         # Windows startup
│  └─ Native.cs            # Win32 declarations
├─ installer/
│  ├─ CursorComet.iss      # Inno Setup 6 script (Chinese / English)
│  └─ ChineseSimplified.isl
├─ assets/
│  ├─ app.ico
│  └─ app.manifest
├─ docs/                   # README screenshots
├─ tests/                  # config/color/save tests
│  └─ Tests.cs
├─ .github/workflows/build.yml   # build and release on tags
├─ tools/make-icon.ps1
├─ version.txt             # single source of truth for the version
├─ build.ps1
├─ test.ps1
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
Check that the tray icon still exists and the tray menu shows “Disable effects” (meaning effects are currently on); press `Ctrl + Alt + T`; exclusive full-screen games may cover the overlay — use borderless windowed mode. If it still fails: right-click the game exe → Properties → Compatibility → “Disable fullscreen optimizations”, or turn off “Optimizations for windowed games” in Windows 11 Settings → System → Display → Graphics → Change default graphics settings. Exclusive fullscreen bypasses DWM, so a normal desktop overlay cannot draw over it; this is an OS limitation.

**How do I disable auto-start?**
Uncheck “Start with Windows” in the main window → Behavior, or delete `CursorComet` under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

**How do I switch language?**
Main window → Language; the installer and uninstaller also have a language dropdown. Changes in the app apply immediately and are saved.

**Why does OBS not capture the trail?**
The trail is a per-pixel layered window, which OBS's BitBlt Window Capture cannot capture. Use **Display Capture**, or set the Window Capture method to **Windows 10 (1903 and newer)**; **Game Capture** usually cannot capture it either.

**How do I uninstall the installed version?**
Settings → Apps → Installed apps → Cursor Comet, or run `%LOCALAPPDATA%\Programs\CursorComet\unins000.exe`. The uninstaller removes shortcuts and the uninstall entry; settings are kept by default.

**Will antivirus flag it?**
The app has no network access and no elevation; it is a plain .NET WinForms program. It is unsigned, so some scanners may warn. You can build it yourself with `build.ps1`.

**Multiple monitors?**
Thickness follows the system DPI of the primary monitor; across monitors with different scaling the size may differ slightly, but the position always follows the cursor.

**Can I change the color or curve?**
Edit the colors in `src\Palette.cs`, or the constants at the top of `src\TrailRenderer.cs`, then run `build.ps1`.

## Versioning

- `version.txt` is the single source of truth; `build.ps1` generates the app and installer versions from it.
- Releases are for meaningful milestones only, not for every fix or visual tweak. The current public version is **1.8.0**, with **1.8.1-preview.1** as the latest preview; the local development version is **1.8.33** (standard Win11 title bar + standard controls + custom-drawn Fluent sidebar navigation + targeted refresh + scaled hand cursor + responsive style cards + follow-the-trail small-window rendering, not uploaded). All 1.x versions are development previews, and the next stable milestone is **2.0.0** (rendering architecture and engineering work).
- Old 1.x releases stay as development history; the version number is no longer a progress bar.

## Known limitations

- The main window cards, title bar, style cards and color swatches are now standard WinForms controls; the sidebar navigation remains custom-drawn (icons, selection bar, responsive collapse). It is not Mica and has no dark mode; some native WinForms controls (scrollbars, combo boxes) are unstyled.
- The overlay is now a small layered window that follows the trail and submits only the area it covers; fast movement, 4K and multi-monitor can still be CPU-heavy. This is the part 2.0.0 continues to optimize.
- The main window is manually scaled for the primary display DPI; mixed-DPI multi-monitor overlays may differ.
- The installer is now an Inno Setup 6 wizard; its standard look differs from the custom-drawn main window.
- There are 27 config/color/save/migration tests and CI build checks; code signing and a standalone `.sln` are still missing.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE) (GPL-3.0).

Copyright (C) 2026 Cursor Comet contributors

You may use, modify and distribute it freely; if you distribute a modified version, you must provide the source under GPL-3.0 as well.
