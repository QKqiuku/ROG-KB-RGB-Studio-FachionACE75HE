# ROG KB RGB Studio

<img src="docs/images/app-icon.png" width="88" alt="ROG KB RGB Studio icon">

[简体中文](README.md) · [Downloads](docs/DOWNLOADS.md) · [User guide](docs/USER-GUIDE.md) · [Publish on GitHub](docs/GITHUB-PUBLISH.md)

An unofficial Windows lighting workbench for the wired **ROG Falchion Ace 75 HE, ANSI 83 keys, VID:PID `0B05:1C7D`**: 83 keys, 12 strip segments and the eye lighting region. Derived from [bbfox0703/RSS_II_RGB](https://github.com/bbfox0703/RSS_II_RGB), distributed under GPL-3.0. It is not affiliated with or endorsed by ASUS, ROG or ATK. The application uses its own keycap mark and a generic eye graphic; device names describe compatibility and do not imply brand permission.

## Download and start

Open **Releases** in the repository you are viewing. Both Full and Lite are self-contained Windows 10/11 x64 packages; no .NET installation is required. Full includes GIF conversion. Lite excludes that converter and disables new GIF imports, but still plays existing `.kbanim` and ATK animations. Effects, process audio, key reactions, strip lighting and custom compositions otherwise work the same. See actual release assets for download sizes.

Extract the whole archive, close other software controlling the keyboard lights, then run `Start-Studio.cmd` or `RSS_II_RGB.App.exe`. The application selects 75HE by default and requires manual confirmation of the administrator UAC prompt. Closing or minimizing hides it to the tray. Use “Exit and restore” or tray Exit to quit, including before upgrading. Automatic startup is not provided for 75HE.

## Platform support

| Platform / architecture | Current status |
| --- | --- |
| Windows 10/11 x64 | Full / Lite preview packages available; see validation limits below |
| Windows ARM64 | No native package; x64 emulation has not been verified |
| Windows x86 (32-bit) | Not adapted or packaged; current projects and native dependencies target x64 |
| macOS / Linux | Not supported; no packages available |

Avalonia provides a cross-platform UI, but this project's HID communication, keyboard input monitoring, audio capture and elevated startup currently use Windows implementations. Additional platforms need platform-specific implementations, native dependency checks and hardware testing, rather than just a different publish target. No release dates are promised. .NET 10 itself supports Windows x86; this project's lack of an x86 package is not a runtime limitation ([official platform information](https://learn.microsoft.com/en-us/dotnet/core/install/windows#supported-versions)).

## Features and limits

- Built-in, key-reactive and music effects; GIF/ATK playback; independent region parameters; strip/eye editing; named custom compositions.
- Preset mode plays the chosen preset. Custom editing starts from the current effect. Choose 15/30/45/60 FPS; actual output depends on device and system load.
- The ATK gallery shows pure keyframe works with a confirmed matching layout and retains creator information. Preview does not change hardware lighting. Remote API changes or interrupted pagination can affect results.
- System and process audio are analyzed locally without recording. Process capture requires Windows build 20348+; protected audio may be unavailable.
- Simplified Chinese, Traditional Chinese and English follow the OS language. Preview shows the last successfully transmitted frame, not hardware readback.

Settings: `%LOCALAPPDATA%\RSS_II_RGB\settings-falchion75.json`; assets: sibling `anims` and `user-presets`; rotating logs: `Logs`. Replacing application files does not remove that data. Keyboard input is used for lighting positions and triggers, without saving typed text; another keyboard may trigger corresponding positions.

This is an **unsigned public preview**. Previous user testing covers that person's hardware, firmware and environment only. Administrator launch restored VALORANT key reactions on the user's machine; other environments are not guaranteed. Unplugging, forced termination, power loss or forced OS shutdown can interrupt restoration. Normal exit attempts to restore the original lighting. The app does not change magnetic actuation, polling rate or firmware. Each release still needs physical-keyboard and clean-machine acceptance testing.

## Build and distribute

Install .NET 10 SDK; packaging also requires PowerShell 7.2+:

```powershell
dotnet restore ROG-KB-RGB-Studio.slnx -p:PublishAot=false
dotnet build ROG-KB-RGB-Studio.slnx -t:Rebuild -c Release -p:PublishAot=false --no-restore
dotnet test ROG-KB-RGB-Studio.slnx -c Release --no-build
pwsh -File tools/Publish-Studio.ps1
```

The publisher creates Full, Lite, corresponding source and SHA-256 files without uploading them or overwriting existing output. This distribution uses self-contained, untrimmed builds; older Native AOT scripts are references only.

Preserve [LICENSE](LICENSE), [NOTICE.md](NOTICE.md) and third-party notices, and supply corresponding source with binaries. Inherited source projects, assemblies and shared namespaces retain `RSS_II_RGB`; the solution, new application test project and release assets use the Studio name. The legacy data directory is retained for settings compatibility. GitHub's generated source includes only the tagged commit: commit every intended new file and modification first. Follow the [publishing guide](docs/GITHUB-PUBLISH.md).

Details: [Architecture](docs/ARCHITECTURE.md), [ATK](docs/ATK-GALLERY.md), [Music](docs/MUSIC-AUDIO.md), [75HE protocol](docs/FALCHION75.md), [Release notes](docs/RELEASE-NOTES.md).
