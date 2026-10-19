# Cursor Comet Terms of Use

**Version**: 1.0  
**Effective date**: 2026-10-19  
**Applies to**: Cursor Comet 1.x and later  
**Project**: https://github.com/cwslz/cursor-comet  
**Contact**: https://github.com/cwslz/cursor-comet/issues

## 1. General

1.1 These Terms are between you and the author of the Cursor Comet project (the “Author”) and govern the download, installation, and use of the software.

1.2 The software is released under the GNU General Public License v3.0 (GPL-3.0). These Terms supplement the GPL and do not replace, limit, or restrict any rights granted by the GPL. If there is a conflict, the GPL prevails.

1.3 By downloading, installing, or using the software, you agree to these Terms. If you do not agree, do not install or use the software.

## 2. The software

2.1 Cursor Comet is a local cursor-trail effect for Windows 10/11. It draws a trail, glow, and halo through a transparent overlay.

2.2 The software is free and open source. It has no account system; the current version does not connect to the internet, collect telemetry, or contain advertising.

2.3 The software requires the .NET Framework 4.8 included with Windows. It does not require Python, Node.js, or the .NET SDK.

## 3. License and open source

3.1 The source code is licensed under GPL-3.0. You may use, copy, modify, and distribute the software as permitted by GPL-3.0.

3.2 If you distribute the software or a modified version, you must comply with GPL-3.0, including keeping copyright and license notices and providing the corresponding source code to recipients.

3.3 These Terms do not restrict commercial use, reverse engineering, or redistribution permitted by GPL-3.0. Any conflicting restriction is superseded by GPL-3.0.

## 4. Installation, uninstall, and system changes

4.1 The installer writes the following to your user profile:

- `%LOCALAPPDATA%\Programs\CursorComet` — program files;
- a Start Menu shortcut, plus an optional desktop shortcut if you select it;
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\CursorComet` — uninstall information;
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\MouseTrail` — only if you select “Start with Windows”.

4.2 Settings and error logs are stored in `%APPDATA%\MouseTrail`. Error logs may contain stack traces and local file paths, and remain on your computer.

4.3 The uninstaller removes program files, shortcuts, the uninstall entry, and any autostart entry created during installation. Settings are kept by default; to remove them, delete `%APPDATA%\MouseTrail` manually.

4.4 Installation and uninstallation do not require administrator rights. The installer may close a running instance of Cursor Comet during installation or upgrade. You can uninstall the software from Windows Settings → Apps.

## 5. Acceptable use

5.1 You must comply with the laws applicable to you and must not use the software for illegal activities or to infringe the rights of others.

5.2 You must not disguise or rename the software as malware, or induce others to download a tampered version. If you distribute a modified version, you must clearly identify the changes and comply with GPL-3.0.

5.3 The full-screen overlay, topmost window, global hotkey, and autostart features may conflict with some games, screen recorders, remote-desktop tools, accessibility tools, or security software. If a conflict occurs, disable the relevant feature or stop using the software.

## 6. Privacy and local data

6.1 The software does not collect or upload personal data and contains no network requests, analytics, or advertising components.

6.2 The software stores settings and error logs only on your computer so that it can remember your preferences and help diagnose problems. You may delete `%APPDATA%\MouseTrail` at any time.

6.3 If you report an issue on GitHub, the system information, logs, and screenshots you provide are subject to GitHub’s privacy policy. Do not post passwords, keys, or other sensitive information in a public issue.

## 7. Risk and disclaimer

7.1 THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT.

7.2 TO THE MAXIMUM EXTENT PERMITTED BY APPLICABLE LAW, THE AUTHOR IS NOT LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, OR CONSEQUENTIAL DAMAGES ARISING FROM THE USE OF OR INABILITY TO USE THE SOFTWARE, INCLUDING BUT NOT LIMITED TO LOSS OF SETTINGS, SYSTEM ABNORMALITIES, DATA DAMAGE, GAME OR RECORDING CONFLICTS, ANTIVIRUS FALSE POSITIVES, OR BUSINESS INTERRUPTION.

7.3 The software is a free hobby project. The Author does not promise continuous updates, technical support, or bug fixes. You may report issues on GitHub, but the Author has no obligation to respond or fix them within any particular time.

7.4 Nothing in these Terms excludes any non-waivable consumer rights granted by the law applicable to you. If this section conflicts with mandatory local law, the local law prevails.

## 8. Third-party components and services

8.1 The software depends on Microsoft .NET Framework, which is subject to Microsoft’s license terms.

8.2 The source code, installers, and issue tracker are hosted on GitHub. Your use of GitHub is subject to GitHub’s terms of service and privacy policy.

8.3 The software may include third-party open-source components; their licenses apply to those components.

## 9. Changes

9.1 The Author may update these Terms to reflect changes to the software or legal requirements. Updated Terms will be published in the project repository with a version number and effective date.

9.2 If you continue to use the software after the Terms are updated, you accept the updated Terms. If you do not agree, stop using the software and uninstall it.

## 10. Termination

10.1 You may terminate these Terms at any time by uninstalling the software.

10.2 Sections 3 (License and open source), 6 (Privacy and local data), 7 (Risk and disclaimer), and any other provisions that by their nature should survive will survive termination.

## 11. Miscellaneous

11.1 These Terms are the entire agreement between you and the Author regarding the use of the software. If they conflict with GPL-3.0, GPL-3.0 prevails.

11.2 If any provision of these Terms is held invalid or unenforceable, the remaining provisions remain in effect.

11.3 These Terms are written in Chinese. This English translation is provided for convenience; if there is any ambiguity, the Chinese version prevails.

## 12. Contact

- Project: https://github.com/cwslz/cursor-comet
- Issues: https://github.com/cwslz/cursor-comet/issues
- GPL-3.0 text: https://www.gnu.org/licenses/gpl-3.0.html
