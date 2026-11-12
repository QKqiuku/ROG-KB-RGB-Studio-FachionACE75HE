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
- **Connection monitor**: distinguish missing devices, inaccessible interfaces and lighting conflicts, inspect lighting transmission, retry and copy diagnostics.
- **Live previews**: the keyboard reflects the last successfully sent lighting frame, with an additional continuous canvas for music effects. Choose 15 / 30 / 45 / 60 FPS.
- **Three UI languages**: Simplified Chinese, Traditional Chinese and English, selected from the system language.

## Effect origins

Most built-in presets draw inspiration from online searches, public lighting demonstrations and AI-assisted design, implementation and refinement. Some effects reference Lumo’s public previews and are reimplemented for the keyboard’s limited LED layout, so they may differ from their references.

Effects will continue to be refined based on their appearance on the physical keyboard. Feedback and new ideas are welcome.

## Want more effects?

- **Find animations**: open the in-app **ATK lighting gallery** to browse, preview and import keyframe animations matching your keyboard layout.
- **Share an idea**: submit a PR with an effect description document or implementation. Description-only PRs are welcome: include a reference video or GIF, colors, motion, triggers and adjustable parameters you would like, so the effect can be discussed and implemented.
- **Write your own**: use existing effects as references and share your implementation through a PR. Connect new effects to the preset list, parameters and display names; include tests and check their appearance on a physical keyboard where possible.

<details>
<summary>Preset and effect source paths</summary>

All paths are relative to the project root:

| Content | Path |
| --- | --- |
| Preset categories, lists and parameter capabilities | [src/RSS_II_RGB.App/PresetCatalog.cs](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/blob/main/src/RSS_II_RGB.App/PresetCatalog.cs) |
| Effect identifiers and display names | [EffectChoice.cs](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/blob/main/src/RSS_II_RGB.App/EffectChoice.cs), [L.cs](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/blob/main/src/RSS_II_RGB.App/L.cs) |
| Regular effects and some animation algorithms | [PatternLayer.cs](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/blob/main/src/RSS_II_RGB.Core/Effects/Layers/PatternLayer.cs), [PatternLayer.Lumo.cs](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/blob/main/src/RSS_II_RGB.Core/Effects/Layers/PatternLayer.Lumo.cs) |
| Key-reactive effects | [ReactivePatternLayer.cs](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/blob/main/src/RSS_II_RGB.Core/Effects/Layers/ReactivePatternLayer.cs) |
| Music effects | [MusicLayer.cs](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/blob/main/src/RSS_II_RGB.Core/Effects/Layers/MusicLayer.cs) and sibling `MusicLayer.*.cs` files |
| Custom strip and eye effects | [DecorationLayer.cs](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/blob/main/src/RSS_II_RGB.Core/Effects/Layers/DecorationLayer.cs) |
| Effect creation and composition | [src/RSS_II_RGB.App/KeyboardController.cs](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/blob/main/src/RSS_II_RGB.App/KeyboardController.cs) |
| Effect tests | [tests/RSS_II_RGB.Core.Tests/](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/tree/main/tests/RSS_II_RGB.Core.Tests/), [tests/ROG_KB_RGB_Studio.App.Tests/](https://github.com/QKqiuku/ROG-KB-RGB-Studio-FachionACE75HE/tree/main/tests/ROG_KB_RGB_Studio.App.Tests/) |

</details>

## Supported keyboard and alternatives

This project currently targets only the **wired ROG Falchion Ace 75 HE with the ANSI 83-key layout**. Compatibility with other ROG models or keyboard brands is not guaranteed.

If you have an unsupported ROG keyboard and would like to help add support, contact me on **QQ: 2033239928** and include the exact keyboard model.

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

## Connection troubleshooting

Click **Connection monitor** beside the status indicator to inspect the connection state, last detected firmware, last successful lighting transmission, frame count, error and detected lighting programs. Connection details update every second while the panel is open. Use **Check again** to rescan running lighting programs.

| Status or symptom | Meaning and action |
| --- | --- |
| Waiting for wired keyboard | No usable target device was found. Check that the model is the supported 75 HE, use a USB data cable, and check the port or reconnect the cable. |
| Keyboard found; cannot open | The device was detected, but its control interface could not be opened. Confirm the administrator prompt was accepted, check other lighting tools and their background processes, then retry. |
| Lighting control conflict | Software lighting mode is already active. Another controller may have enabled it, or an abnormal exit may have left it active. Reading firmware successfully does not mean lighting frames can be sent. |
| Keyboard communication failed | A query or lighting write failed. Check the USB connection and the specific diagnostic error, then retry. |
| An older version keeps searching | Older status text may hide control conflicts or communication failures. Update to a version with Connection monitor and follow the specific status. |

For a lighting control conflict:

1. Use Exit and Restore in this app and exit other keyboard-lighting tools. Check their background processes if necessary.
2. Unplug and reconnect the keyboard USB cable to clear any residual control state.
3. Open only this app. If it is still running, click Reconnect in the monitor. Reconnect retries the connection without forcing ownership.
4. If the problem persists, use Copy diagnostics and include the result, keyboard model, Windows version and reproduction steps in your report.

Detecting LightingService, Armoury Crate processes, OpenRGB or another listed program only means it is running; it does not prove that it owns the keyboard. This app does not automatically stop other programs or override an active software lighting mode. Connected status is shown only after the first complete lighting frame is sent successfully.

Based on [bbfox0703/RSS_II_RGB](https://github.com/bbfox0703/RSS_II_RGB), licensed under [GPL-3.0](LICENSE). Not an official ASUS, ROG or ATK application.
