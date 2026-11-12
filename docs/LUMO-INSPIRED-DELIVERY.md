# Lumo-inspired workbench extension — 2026-11-11

Reference: https://lumo.tozero.me/ and its publicly served demonstration catalogue.
The website presents free and commercial Pro capabilities. No executable, paid
asset, pre-rendered effect pack, activation mechanism or third-party application
code was incorporated. The effects below are independently implemented mathematical
renderers, not a claim of exact visual parity with Lumo.

## Delivered

Thirty additional presets: Aurora, Ocean, Plasma, Nebula, Fire, Candle, Meteor,
Radar, Heartbeat, Checker, Snake, Scan band, Neon border, Wipe, Marble, Silk, DNA,
Sunset, Fireflies, Raindrops, Heart, Heart ripple, Galaxy, Tunnel, Kaleidoscope,
Lava, Bubbles, Lightning, Diamonds and Neon pulse.

Each renderer uses the active keyboard profile rather than a fixed 75% pixel
buffer. Presets reuse existing speed, brightness, colour and saturation controls;
width/density controls appear for effects that use them. Spatial effects support
horizontal/vertical mirroring and 90-degree rotation. Effects based on key indices,
fixed digit grids or individual key identity hide inappropriate transform controls.
Transforms are saved independently per effect. Rotation uses transformed coordinates
for heart/polar effects rather than displaying a no-op button.

The library now supports persisted favourites, the last eight distinct procedural
presets, and random selection excluding Off/GIF/ATK/current effect. ATK and GIF
entry buttons remain near the top of the effects pane. Existing preset/custom
mode isolation, reactive/audio overlays, per-effect parameters, GIF import, local
ATK caching and active-keyboard resource filtering are retained.

Existing selected effect, zones, GIF paths, ATK cache, mode and FPS were compared
before/after deployment and preserved. The default/max target remains 60 FPS.

## Verification

- Complete Release rebuild: zero warnings, zero errors.
- 236 automated tests pass: 192 Core and 44 App.
- Every new effect produces nonblack animated output over sampled time points.
- Multi-time output signatures differ for all thirty effects on both Falchion75
  and Scope II layouts; they are not thirty labels pointing to one renderer.
- Mirror output, actual heart rotation, library persistence, recent-history bound,
  frame-rate preservation and saved-zone retention have regression coverage.
- Deployed framework-dependent Windows build opens, connects to firmware 1.00.50
  and continues sending the user's existing effect at approximately 60 FPS.
- No new firmware/input/remapping command was introduced.

## Remaining work — not implemented in this delivery

This is not a full, one-to-one migration of the advertised 174 effects or the
entire commercial application. Remaining capabilities include:

- Exact reproduction of all remaining ambience/reactive/heart/music variants.
- Screen-region colour capture and per-application audio-source filtering.
- A keyframe/timeline authoring studio, scrolling text and interactive games.
- Movable effect origin, independent secondary procedural-layer controls and
  adjustable key-down/key-up envelope timings beyond the current overlays.
- Automatic music-mode transitions and idle pause/resume behaviour.
- Battery integration, simultaneous-device management and cross-device sync.
- Fn layers, macros, application launch actions and key remapping.

Several items require new capture/input/device-lifecycle subsystems, not merely
additional preset entries. Current support remains the tested keyboard adapters;
adding a future ROG device requires its protocol/layout work and declared form
factor. The source gallery must keep matching the active profile's form factor.

Physical appearance of the thirty new algorithms has not been individually
owner-reviewed. Long-duration gaming/input-latency testing remains outstanding.

## Public-preview comparison follow-up - 2026-11-11

The first thirty renderers were independent approximations from descriptions,
not frame-matched reproductions. The follow-up reads the public website player
metadata and its delta-encoded RGB demonstration frames for research: 278 clips,
each with 96 frames at 12 FPS (eight seconds). These clips do not reveal the
original algorithms, random seeds, parameter response or longer-term behaviour.

Research visually compared 24 directly named matches. Eighteen renderers were
revised against observed shapes, palettes and timing: Ocean, Candle, Meteor,
Heartbeat, Checker, ScanBand, Wipe, Marble, Silk, Sunset, RainDrops, NeonPulse,
Nebula, NeonBorder, Dna, Fireflies, Lava and Lightning. Examples include independent
pastel meteor trails instead of a diagonal strip, rose-gold silk and pale marble,
full-board colour replacement with a bright edge, per-key blue raindrop fades,
dark rests between red heartbeat pulses, moving yellow-green fireflies,
a circulating border with a black interior, and a lit blue/purple ring field.
Candle noise interpolates between samples for smooth high-frame-rate output.

These are characteristic-level reconstructions, not pixel-exact matches.
Comparison sheets still show differences in phase, noise, geometry, colour
transitions and motion. Aurora, Plasma, Fire, Radar, Snake and Kaleidoscope were
inspected but retain their prior approximation. Heart, HeartRipple, Galaxy,
Tunnel, Bubbles and Diamonds have not been matched to directly corresponding
public clips. Complete Lumo parity has not been achieved.

Only independently written procedural renderers ship. Downloaded preview frames,
metadata, scripts and comparison sheets remain outside the product in
`.research/lumo`; they are not runtime assets or a required network service.
The website recording's 12 FPS does not cap the local 15/30/45/60 FPS renderer.

PresetScroll and ParameterScroll are sibling viewport-constrained scrollers.
Both were checked in the published app; the keyboard/header stay fixed.
Regression checks cover pastel highlights, full-board wipe, meteor trails,
heartbeat rests, neon background, dark border interior, nebula brightness,
monochrome animation and raindrop width. They check observed characteristics,
not equivalence with an unknown original algorithm.

Follow-up verification: complete Release rebuild with zero warnings/errors; 249 tests pass (205 Core, 44 App). Published DLL hashes match the tested build. The updated app reconnects to firmware 1.00.50 and reports approximately 60 FPS with the existing Meteor selection; the complete serialized settings remain unchanged across deployment. Physical colour fidelity and input latency were not measured in this follow-up.

## User-requested removal and redesign - 2026-11-11

This section supersedes the earlier available-preset list. Removed Plasma,
NeonBorder, Fireflies, Galaxy, Tunnel, Kaleidoscope, Bubbles and Diamonds from
the library, random selection, Core enum/renderers and active mappings. The App
retains their enum tokens solely to deserialize old settings without discarding
unrelated preferences. On restoration, removed global/custom/zone effects fall
back to Rainbow, retired favourites/history/options are cleared, and other
settings remain intact.

Eight retained presets were revised for the six-row Falchion75 display:
- CountdownFireworks: centred 3x5 digits count 10 through 1 (one second each at
  speed 1), followed by 16 seconds of staggered launches/bursts from varying
  positions; the 26-second sequence repeats. Width changes particle spread and
  density changes ray count. The timeline starts when the effect is selected.
- Aurora: separate green/purple winding curtains with stronger light/dark motion.
- Ocean: moving cyan crests and foam over a dark-blue background.
- Fire: varying red/orange tongues with small warm hotspots, rather than a broad
  persistent yellow bottom band.
- Marble: travelling dark veins and pale highlights, with actual brightness motion
  at speed 1 in both natural-palette and selected-colour modes.
- RainDrops: blue falling heads with fading tails and bottom-edge splashes;
  density changes active lanes and width changes tails/splash extent.
- Heart: shared compact two-lobe/notch/tip silhouette, red double-pulse brightness.
- HeartRipple: the same signed-distance silhouette expands as a hollow outline.

Aurora/Ocean/Fire/Marble now expose their effective width control. Retained
single-colour and transform behaviour is unchanged where applicable. Offline
contact sheets were inspected using all 83 physical-key locations; numeric tests
cover the ten digit masks, changing burst origins, default-speed brightness motion,
limited yellow, falling trails, heart silhouette/outline and old-settings migration.
Physical appearance is still subject to keyboard diffusion and keycap differences;
no claim of measured input-latency or exact Lumo parity is made.

Removal/redesign verification: complete Release rebuild with zero warnings/errors; 257 tests pass (205 Core, 52 App). Reviewed for compatibility, parameter handling, bounds and performance; all eight removed selections have migration coverage. No firmware protocol or keyboard input handling changed.

## Ocean/fire follow-up and heart removal - 2026-11-11

Ocean replaces the narrow crest/foam stripes with the earlier broad sinusoidal
blue/cyan surface plus a slower undertow. Brightness stays above 22% across the
surface instead of leaving large dark gaps; width stretches the waves.
Fire no longer clips against a flame-height threshold: a moving ember component
and rising turbulent tongues cover all rows, including the top two. The palette
still favours red/orange with limited yellow hotspots. Width controls tongue scale.
Removed Heart and HeartRipple through the same catalogue/settings migration as
the previous eight removals. Their Core renderers and shared shape helper are
removed; only legacy App enum tokens remain for deserializing saved settings.
Heartbeat (the separate whole-board red pulse) remains available.
Regression checks require continuous sea brightness, visible time-varying light
in every top-row key across widths .25/1/3, and all ten retired-effect migrations.

Ocean/fire follow-up validation: complete Release rebuild, zero warnings/errors; 258 tests pass (204 Core, 54 App). Eight-second offline samples were rendered at the actual 83 key locations and visually inspected for broad sea flow and light in both top rows. Physical appearance is not yet user-confirmed.

Fire contrast follow-up: change the heat-to-brightness exponent from .65 to 1.35, deepening low-temperature shadows while retaining bright hotspots and moving light in both top rows. Regression checks require red values below 15 and above 170 across sampled frames, with every top key retaining nonzero changing light.

## Requested Lumo batch - 2026-11-11

All 27 requested names are represented: 18 new ambient choices, the reworked
existing SineWave now labelled Sine curve, and 8 independently selectable key
interaction layers. New ambient choices: Monet, Marquee, LakePixels, LakeRipples,
LakeSurge, PerimeterFlow, RotatingRing, SpiralRing, DoubleBeacon, SineSectors,
MulticolorSineSectors, WindmillCycle, Caustics, Sun, Sakura, IceCrystal,
AuroraRipples and Nailong. Sine curve is a moving rainbow line; Sine sectors and
multicolour sine sectors are distinct rotating angular fields. The removed
ambient Heart/HeartRipple and other retired choices remain removed.

Key interactions have their own category and custom-mode panel: Spotlight,
DarkRipple, DoubleRing, Diamond, Dye, Heart, Lightning, Fireworks. Selecting one
preserves the current ambient base and enters custom mode. Ordinary preset
selection pauses all custom layers as before. Speed, range, intensity, natural
palette and picked colour are saved independently per interaction choice. Zero
intensity removes the interaction from composition. DarkRipple multiplies the
underlying colours; Dye replaces them with a spreading retained colour; the other
six overlay lit accents and expire. Key heart grows from the real key position;
it is not the removed autonomous heart preset. The existing legacy flare+ripple
option remains selectable. These are whole-keyboard interaction layers; the zone
editor's previous choices are retained without adding a new zone-state subsystem.

The render engine's existing real-key hits are reused; the keyboard hook and
firmware/input protocol were not changed. Transient history is bounded to 32
hits and expires. Dye state is cached across unrelated rebuilds, cleared when
switching interaction kinds, and does not expire after the colour has settled.
No key names, text or keystroke history are written to disk.

Monet samples the primary display locally into an immutable 16x9 RGB field at
10 Hz, only while it is the connected keyboard's active effect. The render thread
only reads the field and produces blurred/desaturated colours with temporal
smoothing. No screenshots are saved or uploaded. Selection changes/disconnects
stop sampling; DCs/bitmaps are released with bounded shutdown. Multi-monitor
selection and protected-surface capture are not implemented. Width controls blur,
speed controls response, and native palette/selected-colour modes are supported.

All public reference clips were inspected, including single-hit samples from the
website's key-effects section. Procedural renderings at actual Falchion75 key
positions were compared side-by-side. Brightness mismatches in Caustics, Sun,
IceCrystal and DoubleBeacon were corrected against sampled reference means;
Marquee stripe width and key-lightning visibility were also adjusted. These are
independently inferred algorithms, not exact frame/phase/random-seed copies.
Source demonstration frames remain in the research directory, outside the app.

Requested-batch verification: complete Release rebuild with zero warnings/errors; 346 tests pass (289 Core, 57 App). Includes interaction parameter edges, state retention/reset, preset/custom isolation, source-driven screen filtering and all retained layout checks. Framework-dependent deployment is staged for UI/device observation.

Published UI/device observation: firmware 1.00.50 connected at approximately 60 FPS. Independent key controls fit the workbench; dye changed the last-sent-frame preview from blue to retained new colours under live input. Monet produced a nonblack desktop-derived field. The user then began comparing previews and selected DoubleBeacon, so their live selection was preserved instead of restoring the backup. Prior FPS, zones, GIF and ATK paths remain intact. All three published app/Core/Windows assemblies match the tested build. CPU-only 83-LED measurements with 32 simultaneous hits were approximately .02-1.33 ms per frame by key effect; this is not input-latency measurement. Physical colour fidelity is not confirmed.

Mouse-only effect selection fix: preset buttons (including favourites/recent/custom), Random, and base/key/zone effect pickers no longer take keyboard focus. No global lighting hotkeys were registered; normal control keyboard activation caused the reported accidental switching. Key hooks and interaction effects remain intact. Complete Release rebuild: zero warnings/errors; 346 tests pass. Published UI observation: mouse picker works; Down/Space/Return after picking the current interaction did not change either selected base or key effect. Existing settings were preserved. Firmware Fn shortcuts are outside this change.


## 2026-11-11 preset cleanup (supersedes the classifications above)

Monet, Rain, Waterfall and Bloom are retired from selection, favorites, recent
history and old saved setups; their enum values remain stable. Retired base
selections fall back to Rainbow using the existing migration policy. Monet
screen sampling is no longer selectable or activated by a direct legacy base
selection. Ripple is now Key ripple in the key-effects category. Active and
inactive saved custom Ripple bases migrate to an Off base plus the Ripple
interaction, preserving existing per-effect settings.

All retained Lumo-inspired ambient effects now live in Regular. Animations
contains only BrickBreaker, CountdownFireworks and Vortex. GIF import remains
available. Imported ATK metadata is shown only in the active ATK detail panel,
never as a stale footer below the preset list.

The interaction panel adds Rainbow alongside Use effect palette; these modes
are mutually exclusive. Clearing both enables the custom colour picker. The
choice is saved separately for each interaction. Key Fireworks and Countdown
Fireworks share FireworkBurst, including rocket rise, radial particles, gravity
and fade; key hits determine the origin and restart timing.

Sine sectors (the UI's Sine wave, distinct from Sine curve) was reconstructed
from Lumo preview frames: curved radial/angular phase, a dim brightness floor
and the reference pink palette replace the former zero-clipped angular field.
Normalized single-colour brightness RMSE across 9696 reference samples improves
from .592 to .137. This is an independent procedural approximation, not the
original algorithm or exact timing. Multicolour uses the same brightness field;
its hue field has not received a separate reference fit. Reference frames and
analysis remain outside the shipped application.

Verification: complete Release rebuild, zero warnings/errors; 392 tests pass
(319 Core, 73 App). A headless render checked the new controls, bottom-of-list
categories and inactive ATK metadata visibility without desktop or hardware
control. Package: dist/falchion75-preset-cleanup. Physical colour fidelity has
not been checked in this update; the running workbench was left undisturbed.
