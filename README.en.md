# ROG KB RGB Studio

<img src="docs/images/app-icon.png" width="88" alt="ROG KB RGB Studio icon">

[简体中文](README.md) · [Download](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/releases) · [User guide](docs/USER-GUIDE.md)

An unofficial lighting studio for the **ROG Falchion Ace 75 HE**, controlling its 83 keys, 12 top-strip segments and ROG eye lighting zone.

## Why this project exists

For my needs, Armoury Crate feels too bulky, with extensive device-management and ecosystem features. Its lighting workflow emphasizes audio synchronization and multi-device effects, which offer limited value to someone without a full ROG setup who mainly wants to customize a single keyboard.

For keyboard lighting specifically, the available presets and customization options did not meet my needs. The web-based driver is convenient, but its lighting selection is also limited. I wanted to combine effects freely, create key regions, control the strip and eye separately, and save my own compositions.

That led me to build ROG KB RGB Studio on an existing open-source project, focusing on lighting creation for a single keyboard: more presets, finer controls, region and layer composition, key-reactive effects, music sync and keyframe animation playback. You do not need a room full of RGB devices to make more of your keyboard’s lighting.

## Features

- **Built-in effects**: waves, fire, aurora, fireworks and more, with colors, speed, brightness, effect-specific settings and a reset-to-defaults button.
- **Key-reactive lighting**: ripples, spotlights, color spreading, lightning and fireworks triggered by physical key presses, usable alongside a base effect.
- **Music sync**: spectrum bars, a classic equalizer, sound waves, Solar rhythm and more. Capture system audio or a chosen application process, then adjust sensitivity, decay and colors.
- **Custom compositions**: click or drag-select keys into regions, give each region its own colors, effects and settings, and combine custom layers with a base effect.
- **Strip and eye control**: follow the keys below, calculate effects at independent positions, or edit them separately in Custom mode. The strip also supports individual segment selection and music sync.
- **Overall effects**: control the opacity of a complete custom composition with steady output, breathing or beat flashes.
- **My Effects library**: name and save complete compositions, load them like presets, then edit, save or delete them.
- **ATK lighting gallery**: browse, preview and import matching-layout keyframe animations, sorted by newest or hottest.
- **GIF animations**: Full imports, crops and converts GIFs; both versions play previously converted animations.
- **Live previews**: the keyboard reflects the last successfully sent lighting frame, with an additional continuous canvas for music effects. Choose 15 / 30 / 45 / 60 FPS.
- **Three UI languages**: Simplified Chinese, Traditional Chinese and English, selected from the system language.

## Effect origins

Most built-in presets draw inspiration from online searches, public lighting demonstrations and AI-assisted design, implementation and refinement. Some effects reference Lumo’s public previews and are reimplemented for the keyboard’s limited LED layout, so they may differ from their references.

Effects will continue to be refined based on their appearance on the physical keyboard. Feedback and new ideas are welcome.

## Supported keyboard and alternatives

This project currently targets only the **wired ROG Falchion Ace 75 HE with the ANSI 83-key layout**. Compatibility with other ROG models or keyboard brands is not guaranteed.

If your keyboard is not supported, consider exploring [Lumo](https://lumo.tozero.me/). It offers lighting effects, key-reactive effects and music synchronization, and its public previews inspired some effects in this project. Check its [device compatibility page](https://lumo.tozero.me/srgb.html) for your exact model, connection method and feature limitations before use. Lumo includes both free features and paid Pro features.

## Download and start

Currently supports **Windows 10/11 x64 and the wired Falchion Ace 75 HE ANSI 83-key layout**. Windows ARM64, 32-bit Windows, macOS and Linux packages are not available. No separate .NET installation is needed.

Choose a package from [Releases](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/releases):

| Version | Choose it when… |
| --- | --- |
| **Full** | You want to import and convert new GIFs |
| **Lite** | You want a smaller download without GIF conversion; other effects, ATK and existing animation playback remain available |

1. Extract the entire archive; keep the EXE with the accompanying files.
2. Connect the keyboard and close other software controlling its lighting.
3. Double-click `RSS_II_RGB.App.exe` or `Start-Studio.cmd` and manually confirm the administrator prompt.
4. Wait for the connection, then select a preset to start.

## How to use

- **Presets**: choose a regular, reactive, music or animation effect and adjust its parameters on the right.
- **Custom compositions**: enter Custom, choose a base effect (or Off), select keys and create regions. Open the region effect settings to adjust the effect and palette independently.
- **Decorative lighting**: switch the strip or eye to Custom and select its zone to edit. Strip segments can also be selected individually.
- **Save your work**: enter a name in My Effects and save. Load, edit and save it again whenever you like.
- **Music effects**: choose a music preset, select system audio or a target process, then adjust sensitivity for your music. Process capture requires Windows build 20348+.
- **Exit**: closing or minimizing hides the app to the tray while playback continues. Use Exit and Restore or tray Exit to stop and attempt to restore the original lighting.

Exit normally before upgrading or switching between Full and Lite. Settings and assets are stored under `%LOCALAPPDATA%\RSS_II_RGB`; preserve that directory to keep your compositions and animations. See the [user guide](docs/USER-GUIDE.md) for more.

Based on [bbfox0703/RSS_II_RGB](https://github.com/bbfox0703/RSS_II_RGB), licensed under [GPL-3.0](LICENSE). Not an official ASUS, ROG or ATK application.
