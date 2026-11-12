# Process Audio and Music Workbench Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans or subagent-driven-development to implement this plan task-by-task.

**Goal:** Capture system or selected-process audio and drive 38 music presets, with common analysis for ATK audio scenes.
**Architecture:** Extend existing SensorsHost capture/IPC, Core sensor state and layer composition. Keep native capture out of Core; use one latest audio snapshot for all music and decorative layers. Extend the current workbench without disturbing live hardware.
**Tech Stack:** .NET 10, Avalonia, NAudio 2.3, Windows process loopback, source-generated JSON.
**Spec:** docs/superpowers/specs/2026-11-11-process-audio-lumo-design.md

## Global Constraints

- Windows process mode requires build 20348; system capture remains available.
- Existing frame rates remain 15/30/45/60; no vendor edits, no input/HID changes.
- Public Lumo frames are references, never live audio replacements.
- Code/comments English, strings localized in L.cs, native APIs only in Windows/helper.
- Preserve existing dirty workspace; no automatic live restart, desktop input or keyboard tests.

## Review Focus

- Target exits or PID is reused: clear audio and never switch to all-system capture.
- Opposite-phase stereo: preserve power and independent channels.
- Silent/stale input: no newly spawned events or continuing bright animation.
- Main/decorative stacks: independent bounded histories read shared trigger IDs.
- Source/capture failures: show accurate status and preserve source choice.

### Task 1: Common contracts, capture and analysis

Files: Core/Audio/*, Core/Sensors/SensorState.cs, Core/Ipc/SensorContracts.cs,
SensorsHost/AudioProvider.cs and new capture/control files, Windows/NamedPipeAudioControl.cs.
- [x] Add platform-neutral AudioFrame, source/options/control DTOs and IAudioControl.
- [x] Test deterministic silence, tones, stereo inversion, transients, stale/reset and bounds.
- [x] Implement 2048 Hann/512 hop analysis and publish immutable packed snapshots.
- [x] Add native process-tree capture, source enumeration and bounded control IPC.
- [x] Verify target identity, exit behavior, system device recovery and transport cancellation.

### Task 2: 38 music renderers

Files: Core/Effects/Layers/MusicLayer.cs and MusicKind.cs; Core.Tests/MusicLayerTests.cs.
Consumes AudioFrame (SensorState.Audio) and PatternOptions; exposes MusicLayer.
- [x] Test every preset with deterministic audio fixtures and silence.
- [x] Review public frame shapes, implement distinct spectrum/envelope/onset/waveform families.
- [x] Bound histories, reset on generation changes, consume trigger IDs independently.
- [x] Verify physical normalized geometry and negative-row decorative rendering.

### Task 3: ATK audio compatibility

Files: Core/Animation/AtkScene.cs and audio helpers, AtkSceneLayer.cs,
App/AtkGalleryClient.cs and preview renderer, respective existing tests.
- [x] Test real-shape amplitude_bars, stereo_spectrum, AudioFrequency and mixed keyframes.
- [x] Render with optional AudioFrame; add accurate supported/unsupported classification.
- [x] Keep scene/layout/resource validation and preserve existing keyframe behavior.

### Task 4: Workbench integration and settings

Files: App/EffectChoice.cs, PresetCatalog.cs, MainViewModel.Music.cs,
MainViewModel.cs, MainWindow.axaml, AppSettings.cs, L.cs, KeyboardController.cs, App.axaml.cs.
- [x] Append persisted choices, add Music catalogue and localized names.
- [x] Bind selected-process source controls, capture status/meter and shared response parameters.
- [x] Integrate music base layers, per-effect settings, custom layers and source lifecycle.
- [x] Test source selection/persistence and mode isolation, then headless layout verification.

### Task 5: Verify and ship

- [x] Full Release Rebuild and all test suites; review new state/IPC/COM paths.
- [x] Offscreen UI and synthetic audio source checks without real UI/hardware interference.
- [x] Publish independent folder; verify tested/shipped assembly hashes and helper files.
- [x] Update launchers and document limitations, reference deviations and untested hardware.


## Execution evidence

Full Release Rebuild: zero warnings/errors. All542tests pass (465Core+77App).
Offscreen ROG-theme render passed with the source panel,38music choices and
independent scrolling. Native own-process silent activation initially failed
E_ILLEGAL_METHOD_CALL because a private COM interface hid IAgileObject; public
COM interfaces and an explicit QueryInterface check fixed it. The isolated
activation test passed twice in the implementer plus once in final integration.
No running workbench restart, desktop input, keyboard write, playback routing
change or arbitrary application audio capture was used for verification.

Ruling: keep process capture in the installed non-AOT helper and NAudio2.3; native
COM activation extends the existing platform boundary without a new dependency.
Ruling: independent public-reference geometry drives all38presets with actual
audio snapshots, because the website supplies fixed simulated previews and no
original DSP source; exact equivalence remains unclaimed.
Ruling: reject absolute-position ATK AudioFrequency exports missing device canvas
placement; guessing1000x300would misrepresent compatibility. Auto-layout support
remains available with documented sampling approximation.
Ruling: source/analysis options are shared while palette/spatial options remain
per-preset; changing unrelated frame-rate/brightness settings preserves history.
External review found render-count-dependent ATK smoothing; replaced it with
elapsed-time coefficients and15vs60FPStrajectory regression cases. Followup
fixed equalizer/fire palette directions,quantizedwhitecaps and right-only
waveforms. Physical two-app isolation,visual fidelity,CPU/input latency and device
recovery on the owner's setup are explicitly unverified.
