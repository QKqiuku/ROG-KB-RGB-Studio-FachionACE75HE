# Process audio capture and Lumo-inspired music effects

Status: design for user review; implementation has not started.

## Requested result

Add a Music category with the 38 publicly previewed Lumo music effects. All are
reactive to live audio. Include a source picker for system output or one target
application/process tree. Preserve preset-only versus custom-base-plus-layers
behavior, 15/30/45/60 FPS selection, current keyboard mapping, decorative lighting
modes and last-sent-frame preview. No real desktop automation or hardware writes
are part of development verification unless separately arranged with the owner.

## Evidence and fidelity boundary

Public source: https://lumo.tozero.me/ (retrieved 2026-11-11). The current site
exposes a 38-item Music catalogue. Its app.js states that the demonstration is
96 prerecorded frames at 12 FPS from simulated music; it does not expose the
real-time desktop DSP or a process picker implementation. These public frames
are visual references only and must not become runtime animation playback.
The project independently implements audio-driven renderers inferred from those
references; it must not promise original source algorithms or exact equivalence.
No application binary extraction, license bypass or remote script execution.

Microsoft process-loopback reference:
https://learn.microsoft.com/en-us/samples/microsoft/windows-classic-samples/applicationloopbackaudio-sample/
The OS API captures a selected PID and children independently of output endpoints;
minimum supported Windows build is 20348. Unsupported OS versions retain system
output mode and explain why process mode is unavailable.

## Source selection and lifecycle

The Music settings panel offers System output and Selected application, a
refreshable application list, and readable capture status plus a signal meter.
List audio-session-owning processes first, allow selecting another running app,
and display application name and PID to distinguish instances. Exclude this app
and its helper from the ordinary list. Handle inaccessible process metadata.

Selected-app capture uses the Windows process-loopback API with include-tree
semantics, which covers browser/player children. It does not inject code into
the target, mute other apps, reroute playback, install a virtual device, or alter
Windows audio settings. It captures that process tree, not an individual browser
tab. A target that produces no accessible audio yields silence with status.

Switching the source stops the old capture, clears old analysis/trigger state,
and starts one new capture. Process identity includes PID and start time to
avoid following a reused PID. If the process exits, clear its signal and report
waiting; never silently listen to all system audio. Save the source preference
and executable identity, not a persistent PID. Restore only an unambiguous live
match; otherwise request selection in the panel. Default-device capture retains
its existing device-change recovery. No audio is saved or uploaded.

## Architecture: extend existing boundaries

- SensorsHost owns all capture and analysis. Keep NAudio 2.3 system loopback;
  implement process capture with Windows APIs inside this non-AOT helper using
  the Microsoft sample as API documentation. Do not assume NAudio v3-only APIs
  exist in the installed dependency. Add no native build toolchain dependency.
- Core defines platform-neutral source/status/control contracts and immutable
  analyzed audio snapshots. Windows implements the control client; App binds UI.
  Keep the existing sensor data pipe; add a bounded request/response control pipe
  for source enumeration/configuration/status. One local capture serves all
  effects; unrelated metrics polling remains at its existing cadence.
- Existing legacy AudioBands consumers remain compatible. Publish a separate
  richer snapshot atomically so all new effects in a frame see the same audio
  sequence, timestamp, waveform, spectrum, envelope and trigger counters.
- KeyboardController builds music layers using the same effect stack. Main and
  decorative spatial stacks consume the same trigger counter independently;
  a render must never destructively consume a beat event.
- AtkScene gains validated supported audio layers using this common analysis.
  Do not weaken layout matching, source version checks or resource budgets.

Control/data JSON uses existing source generation. Bound request sizes, enum
values, array lengths, finite numbers, process IDs and timeouts. Keep capture
errors visible instead of converting them to a misleading silent success.

## Audio analysis

Preserve left/right PCM before any averaging. Combine spectral powers rather
than averaging stereo waveforms, avoiding cancellation of opposite-phase audio.
Keep actual RMS/peak envelope separate from normalized frequency bands.

Start with a 2048-sample Hann FFT and 512-sample hop, sample-rate-aware logarithmic
bands, independent left/right energy, waveform samples, low/mid/high energies,
and positive spectral flux for onset detection. Use a rolling local baseline,
noise floor, hysteresis and a minimum interval to reject repeated hits. Label
this beat/onset response rather than claiming reliable musical BPM tracking.

Use time-based attack/release and optional bounded slow auto gain. Preserve
music dynamics; do not independently force every quiet band to full scale.
Expose sensitivity, gate, attack, release, gain mode and bass emphasis with
conservative defaults. Separate audio delivery (up to 60 snapshots/s) from
metrics and LED FPS; render only the freshest snapshot without queued backlog.
Stale/disconnected data decays to zero and cannot trigger a new event. Every
stateful effect resets on source generation change and time rewind.

## Catalogue

1. 频谱柱
2. 节拍闪光
3. 经典均衡器
4. 镜像频谱
5. 霓光频谱
6. 霓光频谱(流彩)
7. 柔光频谱
8. 声浪
9. 频谱瀑布
10. 律动环绕
11. 冲击波
12. 节拍星火
13. 能量核心
14. 底部能量核心
15. 音量呼吸
16. 律动爱心
17. 频谱彩光
18. 双向频谱
19. 频谱圆环
20. 底部频谱圆环
21. 节拍涟漪
22. 低音上涌
23. 节拍扫光
24. 律动星空
25. 节拍棋盘
26. 律动火焰
27. 波形跳动
28. 中央律动
29. 行音柱
30. 音波扩散
31. 万键穿心
32. S线律动
33. 等离子闪电
34. 狂欢派对
35. 幻彩回廊
36. 音乐水洼
37. 烈焰之眼
38. 放射频谱

The names are user-facing localized strings. Append enum values or use a new
music enum; never renumber persisted legacy effect values. A music-specific
Heart/Fire/Lightning does not restore previously removed ambient presets.

## Rendering rules

Reference review must cover each effect's spatial shape, palette progression,
direction, trails and input mapping before implementation. Families can share
math but must preserve visibly different behavior:

- Spectral: columns/equalizer/mirrored/neon/soft/bidirectional/radial/ring variants
  derive local height/intensity from frequency bands. Neon adds a narrow bright
  edge and glow; soft spectrum has broad blending. Flowing neon changes palette
  over time while its visible energy remains audio-driven.
- Spectral waterfall uses bounded time history of real spectra. It advances by
  analysis time; live silence eventually clears all history.
- Envelope: breathing, heart, central/bottom core and central movement map energy
  to brightness, size and contour. No sound means no new growth or bright pulse.
- Onset: flash, shockwave, sparks, ripples, sweeps, checker, starfield, puddles and
  similar accents spawn or advance from real onset/energy events. Existing
  particles may finish fading after silence. Bound concurrent histories.
- Waveform: waveform/S-line/row-bar variants derive geometry from live waveform
  or band values, not a time-only sine presented as sound response.
- Complex party/fire/lightning/corridor/eye variants use real bands/envelope and
  event counters for intensity and spawning. Time may control trails/hue motion
  but cannot generate endless bright events without audio.

Use normalized physical key coordinates, not HID address order, for patterns.
Decorative Spatial mode evaluates bar/eye positions; Follow mode samples keys.
Preview continues to show the final successfully sent frame, never a separately
simulated decorative animation.

## Workbench and saved settings

Add a separately scrollable Music group to the existing preset selection area.
Selecting it in preset mode uses only that preset. Custom mode can choose a
music base plus existing custom/key layers. Audio capture settings are shared;
effect palette/brightness/spatial options are saved per music preset. Existing
legacy audio overlays remain supported without double-boosting the new presets.

The right Music panel contains source picker, status/meter, effect controls and
an expandable response section. It uses the current ROG dark theme. At minimum
provide effect palette, rainbow and custom colour where meaningful, brightness,
sensitivity and decay; expose direction/width/history where relevant rather than
showing controls that have no effect. Normal keypresses cannot select presets.
Only activate capture while an audio consumer/preview needs it; restart cleanly.
No automatic music/base switching in this iteration; current manual mode behavior
remains predictable.

## ATK audio scope inherited from the earlier request

Support amplitude_bars, stereo_spectrum and AudioFrequency where the complete
scene can be reproduced with supported layers. Preserve keyframes, layer order,
colours/gradients, brightness and supported module parameters. A native static
background may be supported as the small necessary companion for these scenes.
The named spectrum and pulse effects in ATK's current public JS are time-driven;
if added for mixed-scene compatibility, reproduce them as such rather than
incorrectly treating them as audio inputs. Do not claim game-direction support
from left/right volume. Other procedural/game modules remain explicit reasons
for rejection; no silent partial import. Preview shares selected audio source
without applying to the keyboard or changing persisted animation selection.

## Acceptance and delivery

1. Deterministic PCM tests: silence, sine sweep, bass impulses, volume steps,
   left-only/right-only and opposite-phase stereo, multiple sample rates, stale
   data, source changes, NaN/oversized input and bounded gain.
2. Every music preset responds differently to suitable real analyzed fixtures;
   silence creates no continuing bright triggers. Frequency presets distinguish
   bass/treble, envelope presets distinguish volume and triggers fire once per
   onset per independent layer. Test bounded histories and decorative stacks.
3. Settings/source validation, preset/custom isolation, normal-key focus safety,
   source failure/restart and persisted enum compatibility tests.
4. ATK real-shape fixtures, audio/keyframe mixing, unsupported module reasons,
   preview/import parity and unchanged form-factor guardrails.
5. Offscreen UI rendering checks source controls, parameters, all 38 catalogue
   choices, independent scrolling and saved settings without real desktop input.
6. Full Release Rebuild plus relevant full suites; independent package and tested
   assembly hash checks; update launchers without stopping the live workbench.
7. Physical acceptance is a separate arranged step: two simultaneous audio apps,
   selected source isolation, target exit/restart, source switch and silence,
   bar/eye response and measured visual latency/CPU. Report untested device
   behavior and reference mismatches honestly; do not claim zero input impact.

## Explicit limits

The website preview alone cannot establish exact original DSP or all input
responses. Approximate visual reconstruction must be documented effect by
effect. Some protected/exclusive/session-isolated app audio may not be available
through the OS capture API. Process selection is per tree, not per browser tab.
Actual 60-FPS HID and end-to-end delay must be measured, not inferred from the
selected frame-rate value. No change to keyboard input/actuation/firmware logic.
