# Music-reactive lighting and audio sources

## Usage

Select one of the 38 Music reactive presets in the left panel. On the right,
choose System output or refresh and select the application/process whose audio
should drive the lights. Process mode includes descendants (e.g. player/browser
helpers), not an individual browser tab. It does not mute or reroute other apps.
An application that has no accessible output produces silence.

The signal meter shows the current audio envelope. Sensitivity and decay are
immediately available; expand Audio response for automatic gain, attack, noise
gate and bass emphasis. Palette/colour, brightness and applicable spatial/history
controls remain in the effect panel. Controls with no effect are hidden. Response
settings and source preference are shared; effect settings are saved per preset.

Preset mode uses only the selected music effect. Custom mode can use a music
base and add key/custom layers. The bar and eye retain their independent Follow
keys or Spatial choices. Preview remains the final successfully sent frame.

If a selected process exits or its identity changes, the capture clears rather
than switching to system output. Refresh/reselect when the saved application has
multiple matching processes. A unique matching executable may be restored;
persisted PIDs are not reused. Process capture requires Windows build20348+.
Unsupported targets/OS/capture errors are shown in the panel.

## Implementation

SensorsHost owns WASAPI endpoint/process loopback. Process mode uses the Windows
application-loopback API and an agile COM callback, with no virtual audio device
or code injection. A local bounded current-user control pipe selects sources;
existing sensor transport publishes latest-only audio snapshots at up to60Hz,
independent of the keyboard's15/30/45/60 FPS setting.

Audio analysis uses stereo powers (avoids phase cancellation), 2048-point Hann
FFT/512-hop,32 logarithmic bands, RMS envelope, low/mid/high energy and transient
flux/onset IDs. Time-based attack/release and bounded optional gain preserve
relative intensity; this is not a music-transcription/BPM system. No audio files
are written or uploaded. Capture pauses when no music/legacy audio/ATK preview
consumer is active. Old audio-zone consumers still receive24 compatible bands.

MusicLayer implements all38 public Lumo Music catalogue names with actual live
signals. Frequency controls spectrum heights, volume controls core/heart size,
waveform drives contours, and onset IDs spawn flashes/rings/sparks. Event/history
buffers are bounded and reset across source generation/time rewind. Silence
spawns no continuing bright events; existing trails may finish fading.

## Reference fidelity

Public reference: https://lumo.tozero.me/ . The webpage contains prerecorded
12-FPS frames from simulated music, not its desktop DSP source. All38 previews
were inspected at several frames. Renderers are independent procedural
reconstructions; they do not replay reference data at runtime and are not claimed
to be exact copies of original algorithms, timing or palettes. Notable limits:
peak caps are not the original peak-hold implementation; complex party/fire/mesh
shapes use inferred geometry; unknown real-input behavior cannot be recovered
from a fixed demonstration. Source frames/analysis stay outside the package.

## ATK compatibility

The same source can drive ATK amplitude_bars, stereo_spectrum and automatic-layout
AudioFrequency scenes, mixed with keyframes and static backgrounds. Named ATK
spectrum/pulse effects are time-driven upstream and retain that behavior. Other
unsupported enabled modules still reject the whole scene with a bounded reason.

Absolute-position AudioFrequency exports without source-device placement cannot
be reconstructed from the shared JSON. These remain rejected explicitly instead
of claiming support using guessed canvas coordinates. Automatic-layout raster
sampling is approximate; source sub-key antialiasing is not reproduced. Gallery
layout filtering still uses the active KeyboardProfile.FormFactor.

## Verification boundary

Automated checks use deterministic PCM and synthetic snapshots, plus offscreen
UI rendering. A separate helper self-test captures only its own silent process
to verify actual Windows activation, without launching normal sensor/control
pipes, playing sounds or touching keyboard hardware. This proves native
activation, not real-world isolation across arbitrary third-party audio apps.

Still requiring physical acceptance: two-app audio isolation, target app/device
recovery on the user's setup, visual fidelity, CPU and end-to-end/input latency.
The existing workbench is not automatically stopped or restarted during delivery.


## Display response correction

Normal-amplitude synthetic PCM exposed a calibration gap: the first renderer
used linear RMS/band values directly as displayed brightness and geometry, while
horizontal radius coordinates were compressed by1.7. Low raw waveform amplitude
also limited displacement. The previous high-amplitude renderer fixtures missed
this integration behavior.

MusicLayer now maps energy/bands to a monotonic perceptual display response,
preserving exact zero and relative loudness. Waveform geometry uses per-window
peak normalization with a.04floor; actual envelope still controls brightness.
Removed horizontal compression and expanded core/ring/central geometry. Analysis
snapshots,noise gating and ATK source semantics are unchanged.

Using the same normal-level synthetic PCM and default sensitivity, pixels above
the fixed16/255channel threshold changed: EnergyCore6to83, BottomEnergyCore2to49,
SoundWave8to21, SpectrumBars2to12. These are offline comparisons, not measured
hardware luminance. Added five end-to-end PCM/render regressions. Full Release
Rebuild0warnings/errors;547tests pass(470Core+77App). Exact silence/stale data and
bounded tails remain tested. Perceptual mapping may make a release tail appear
longer; decay remains adjustable. No live keyboard or app-source changes were
made during this update. Package: dist/falchion75-music-response-fix.


## Music curation and original-reference comparison

The requested24music choices are retired;14remain selectable. Persisted enum
values are retained, but retired choices are removed from base/zone selections,
favourites,history and per-effect settings. Retired music selections migrate to
SpectrumBars while retaining the saved audio source and shared response options.
The requested neon soft/flowing variant refers to FlowNeonSpectrum (previously
labelled Flowing neon spectrum), now retired alongside SoftSpectrum.

Selecting any retained music effect shows a neighbouring Lumo public reference
canvas. It retains original655x240coordinates,101source keys,96source RGB frames
and12FPS. It does not fit the source to the83-key keyboard, shrink resolution,
remove unlit keys or replace frames with the inferred renderer. Frames are
stored as uncompressed original RGB values in the embedded408074-byte resource;
key-cap shading follows the public canvas. Window default width1480accommodates
both previews; horizontal scrolling preserves reference size on smaller windows.

This user-requested demonstration reference supersedes the earlier policy of
keeping all source demonstration frames outside the application: only the14
retained previews are embedded, with visible attribution. The public website's
simulated-music clip plays separately from the selected live process. It cannot
provide a same-audio,phase-synchronised comparison or reveal the original DSP.
The actual keyboard preview remains the final successfully sent hardware frame.

Verification: full Release Rebuild0warnings/errors;550tests pass(470Core+80App).
Offscreen rendering verifies reference bounds655x240and correctselectedindex,
retained catalogue count and the full side-by-side layout. Resource tests verify
all14source mappings and frame length; deletion migration covers all24retired
choices. Original data provenance/checksums were recorded in research notes.
No live app,keyboard or audio-source changes were made. Package:
dist/falchion75-music-reference.

## Continuous live effect canvas

The primary preview tab now renders the live base effect directly into655x240
pixels,using the selected process audio and current effect options. It is not an
upscaled LED frame or prerecorded reference. RenderCanvas and keyboard rendering
share MusicLayer's temporal preparation and pixel field; tests verify identical
samples on the same grid. The keyboard preview remains the final hardware frame.
Custom overlays are intentionally excluded from the base-effect canvas.

Raster work runs as one cancellable background job at a time,with reusable
buffers and a30FPSvisual cap; invisible or unselected views do not start jobs.
Bitmap publication remains on the UI thread. It reads the engine elapsed clock
when available; audio/beat acquisition and preview scheduling can differ by a
frame,so exact phase/sample alignment is not claimed. It does not increase HID
frame rate or alter the audio analysis resolution. The previous Lumo reference
is preserved in the neighbouring tab.

Verification: full Release rebuild0warnings/errors;553tests pass(473Core+80App).
Offscreen verification produced a nonblack live spectrum from a continuously
updated synthetic source,verified655x240bounds and switched both preview tabs.
The headless harness supplies manual timer ticks because it has no running
desktop event loop. Real-device visual timing and live CPU load remain untested.
Package: dist/falchion75-live-music-canvas.


## Sound wave ribbon remake

SoundWave now follows the supplied visual reference: six overlapping coloured
translucent ribbons rooted on the horizontal centre line,with tapered ends,
bright rims and fine parallel surface contours. The actual audio envelope,
low/mid/high energies and waveform modulate the ribbon profiles; silence is
black. Width controls expansion and Speed controls flow in both palette and
custom-colour modes. This replaces the earlier single narrow waveform contour.

The keyboard and655x240canvas share the same field. Ribbon profiles are prepared
once at257horizontal samples with interpolation,avoiding six trigonometric
calculations for every raster pixel. This is smooth procedural geometry,not
keyboard-resolution upscaling or replay of the supplied screenshot. Fine ribbon
texture inevitably becomes sparse when sampled onto physical keys.


## Sound wave simplification (supersedes the ribbon remake)

The owner's clarified target is two smooth sinusoidal beams with a fixed120degree
phase difference,drifting slowly right while the audio envelope controls amplitude.
Removed the six ribbons,fill,filament texture and frequency-specific surface
profiles. Cyan and magenta distinguish the two beams; custom colour remains
supported. Default motion is one screen width in20seconds,scaled by Speed.
Width controls beam thickness; amplitude follows live audio and exact silence
remains black.

For coarse key rows,the triangular beam profile is integrated over the vertical
sample footprint rather than evaluated at a single centre point. This prevents
a thin beam disappearing between keyboard rows. Display coverage uses a gentle
square-root lift for readability; native canvas pixels use the same integral at
their own footprint size. Tests verify two distinct bands,exact rightward
translation under a stationary signal,amplitude ordering and visibility across
at least12physical columns at multiple phases. This is an algorithm designed
for the low-resolution keyboard projection,not an attempt to reproduce every
fine line in the illustrative picture. No live device test was performed.


## Eleven-preset refinement

BeatRipples,RainbowCorridor and VolumeBreathing are retired. Eleven music presets
remain; enum values stay stable and previous selections/favourites/settings use
the same migration policy. Their original-reference clips are also removed from
the embedded resource,whose validated count is now11.

SoundWave phase separation varies slowly between1.49and2.69radians while both
beams continue rightward. Default strong audio can reach both canvas edges;
amplitude is bounded to half-height and weak audio remains smaller. Keyboard
footprint integration is retained.

ClassicEqualizer uses independent ballistic white peak blocks for its32bands.
Rising columns kick a falling block upward; elapsed-time gravity returns it to
the column with no repeated bounce on a steady level. Native canvas blocks have
square pixel dimensions and gaps; coarse key rows snap to keep them visible.
Source/time resets clear state and silent blocks fall away. Reference for peak
modes: https://github.com/stuerp/foo_vis_spectrum_analyzer/blob/master/docs/README.md

RhythmOrbit keeps the rotating angular highlights,palette and audio-dependent
extent,but its radial profile fills inward so the central black hole disappears.
EnergyCore is a centred circular disc measured in physical pixel/grid units,
with audio-driven radius and soft blue edge around the existing pink centre.
Its transform controls are hidden because a radial circle has no directional
transform; old saved normalized rotations cannot make it elliptical.

The current11original public clips remain an independent recorded reference;
new renderer refinements do not alter those source clips. Visual validation used
synthetic audio,not live hardware. A contact sheet verifies the new square
markers,circle,and filled orbit. Source clipping and key projection remain low
resolution; no claim of exact Lumo DSP or physical visual equivalence is made.


Refinement verification: complete Release Rebuild0warnings/errors;563tests
pass(483Core+80App),including elapsed-time cap flights at15and60FPS,source reset,
coarse-row cap visibility,changing phase separation,defaultstrong-audio edge
reach,circular pixel bounds and removal migration for all27retiredmusic enums.
All11retained reference RGB streams were compared byte-for-byte with their
original public data. Package: dist/falchion75-music-refined. Live two-app audio,
physical colour fidelity and input latency were not retested.


## Spacing, low-frequency triggers and surface corrections

The owner clarified that music changes SoundWave's vertical beam separation,not
its oscillation amplitude or horizontal wavelength. Each beam has a fixed.24
normalized excursion. Audio changes the mirrored baselines within upper world
coordinates[-.2,.4](bar top to QWERTY)and lower[.6,1.2]. Independent rightward
speeds.04and.06ensure persistent phase drift. The live canvas includes this
extended vertical region; physical key/decorative positions sample the same
field. Footprint integration is retained.

AudioAnalyzer now publishes independent BassBeatId/LowBeatId and corresponding
strengths. Bass uses low-band flux; low-weighted events add20%mid flux and ignore
treble. A treble onset over held bass cannot emit a new bass event. Shockwave
uses only BassBeatId and physical circular radius; BeatFlash uses LowBeatId.
General events remain unchanged for other consumers. Numeric snapshot transport
appends four bounded fields(239values);235-value legacy frames decode safely
with no new trigger IDs. Package app/helper assemblies must be updated together.

RhythmFire no longer blends the bottom row toward white. NeonSpectrum uses
SpectrumBars fill/brightness with a flowing pastel palette. EnergyCore retains
its filled circular pink-blue core and adds outward concentric rings/pulse
fronts. RhythmOrbit retains angular rotation and audio strength while removing
the radial mask that left corners dark. Continuous base-effect previews and
keyboard sampling share these changes.

Related public visual references inspected:
https://audiomotion.dev/ (radial visualization)
https://beamtracer.com/docs/viz/concentric-rings (audio-responsive concentric rings)
These are shape references,not dependencies or imported implementation code.

Verification: full Release Rebuild0warnings/errors;581tests pass(501Core+80App).
PCM fixtures include bass/treble at multiple sample rates,treble attacks over
held bass,midrange weighting and noise gate. Rendering checks cover gap versus
amplitude,two rightward velocities,QWERTY/bar world limits,physical ring geometry,
warm fire,pastel spectral fill,core outer rings and lit corners. Visual contact
sheet used synthetic audio; physical colour/latency and actual app isolation
were not retested. Package: dist/falchion75-music-bass-and-spacing.


### 2026-11-12 — wave displacement and audio geometry correction

- SoundWave fixes baselines at y=0.1/0.9 and lets audio displace points up to 0.3 on each side: upper envelope -0.2 to 0.4 (top bar to QWERTY), lower symmetric. Independent rightward speeds remain.
- RhythmOrbit changes angular width and spatial twist with bass/onset, keeping a lit centre and corners.
- Shockwave accepts only bass events at least 750 ms apart, with a raw bass/onset floor of 0.08; source changes and rewind reset the gate. Other presets retain their event cadence.
- NeonSpectrum preserves SpectrumBars geometry and uses broad pastel bands translating horizontally (hue = x*0.28 - time*0.16).
- EnergyCore retains a round pink/blue centre with sixteen attached, tapered spikes whose reach responds to audio; detached rings were removed.
- All checks use synthetic audio/offscreen rendering; no live keyboard or capture settings are changed during development.


### 2026-11-12 — intersecting waves and low-resolution clarity

Supersedes the earlier split-baseline wave and 750 ms shockwave gate:
- SoundWave uses two centre-crossing sine waves (baseline 0.5), phase offset 120 degrees, independent speeds 0.04/0.06. Audio-driven amplitude spans 0.1–0.7, placing the default upper crests between QWERTY y=0.4 and bar y=-0.2; troughs are symmetric.
- NeonSpectrum keeps wide horizontally translating bands but no longer multiplies user saturation by 0.4, reducing the white cast on physical LEDs.
- Shockwave again accepts each bass event; default expansion speed is 2.6 (was 0.65) and ring width is 0.16 (was 0.08), multiplied by the existing user controls.
- EnergyCore doubles the audio-driven spike reach (bass 0.65, spectrum 0.30), with a smaller idle reach.
- RhythmOrbit removes the audio-dependent spiral colour distortion. A thick radial glow expands/contracts with bass while its broad highlight and colour wheel rotate. A dim fill preserves the centre and corners. Low-resolution tests cover the actual 83-key profile.


### 2026-11-12 — wave palettes and Solar rhythm

- ClassicEqualizer cap rendering extends to y=-0.2 (the top light bar) and the live canvas includes that upper band. Physical decorative LEDs require the existing Spatial mode; FollowKeys continues to mirror the function-row keys.
- SoundWave exposes two independent saved colour pickers. Editing either selects custom colours; Rainbow preserves these choices while rendering a continuously travelling rainbow along each wave. Core options carry both colours through the same keyboard/canvas renderer; older settings receive cyan/magenta defaults.
- EnergyCore's persistent enum is unchanged; its display name is Solar rhythm. Attached rays have wider roots and larger bass/spectrum-driven reach; rainbow hue continuously cycles. Custom colours form a related-hue radial gradient.
- RhythmOrbit custom colours use the same related-hue gradient while preserving its audio geometry.
- Vortex is removed from selectable animation presets and uses the existing retired-preset migration/cleanup path for settings, favourites and history.
- Checked with synthetic audio, source-generated settings round trips, actual spatial LED sampling, and headless rendering; no hardware or live audio capture was operated.


### 2026-11-12 — bass waves, full-height spectrum and additive accents

- SoundWave now reads only Bass for displacement and intensity. Its amplitude uses the linear mean-bass range (gain 3.5, clamped) instead of compressed full-band volume, so high mids/treble cannot hold the waves at full amplitude. Silent bass clears both waves; phases, speeds and saved palettes are unchanged.
- All six active spectrum presets (SpectrumBars, ClassicEqualizer, MirrorSpectrum, NeonSpectrum, BidirectionalSpectrum, RhythmFire) include the top strip at y=-0.2. Bottom-up columns use (1-y)/1.2; bidirectional bars span -0.2 through 1.2 symmetrically. Equalizer caps and column heights use matching coordinates. Shared canvas bounds keep the live preview aligned.
- ReactivePatternLayer's lit accents now use saturating additive composition with the base, including spotlight/lightning/fireworks; a fading accent no longer replaces the base with a dim pixel. DarkRipple remains multiplicative and Dye remains an opaque replacement. The shared Over blend mode is unchanged.
- KeyHeart and KeyDoubleRing are retired in the selectable catalogue and saved-state cleanup. Enum tokens remain readable, and invalid saved reactive selections use the existing fallback.
- Synthetic tests cover every physical strip/eye sample for all six spectrum kinds, RGB preservation while accents overlap/fade, bass-only response, and retired-preset restore.


### 2026-11-12 — final opacity, independent regions and orbit beat sweep

- Custom mode adds a final output envelope: Constant, Breathing or bass BeatFlash, with maximum/minimum opacity and speed. 0% turns the entire output off; 100% preserves the composed effect. It runs after key layers, decorations, custom colours and interactions, before the HID write and acknowledged preview snapshot. Preset-only mode bypasses it. Cached envelope state survives unrelated settings changes.
- Clicking the preview strip selects the whole 12-segment strip; clicking the ROG eye selects its separate region. Selection opens Custom mode while keeping the current base preset. Both regions independently save Solid, left-to-right Music meter, flowing Rainbow or Breathing, colour, brightness and relevant speed. These regions are separate from key index zones and cannot alter the 83 key addresses.
- Custom mode is now supported by Falchion75Lighting. Custom decorative layers only mask strip/eye indices and override the spatial frame; FollowKeys and Spatial remain available. Preset-only mode bypasses custom region settings.
- Overall BeatFlash and custom Music regions independently request the existing selected-process/system audio capture, with no fallback to another source. Switching them off releases capture when no other effect needs audio. Music meters use the shared analyser's smoothed volume/bass and grow left-to-right; the eye uses its single-pixel level.
- RhythmOrbit removes bass-dependent radial contraction and colour distortion. The colour wheel rotates steadily; each bass event launches a broad bright ring outward. Its chosen-colour gradient is preserved.
- Validation covers independent persistence, audio capture demand, custom-region isolation, output attenuation across all 96 LEDs, final acknowledged preview ordering, malformed bounds, source resets and 83-key beat sweep visibility. Offscreen UI checks instantiate both custom region editors and selection states without running the hardware lifecycle.


### 2026-11-12 — rainbow music, segment studio and Prism beat

- Strip Music adds a persisted Rainbow palette switch with flowing colours and speed; its meter height and audio intensity remain unchanged. Eye choices are Solid/Rainbow/Breathing only. Old eye Music settings migrate to Solid without discarding the selected colour.
- Strip Custom adds Segment studio (Segmented, appended enum value 4). Its 12 independent positions appear in the lower zones panel as selectors 1–12. They use explicit output indices 83–94, separate from preview source-key indices; the eye remains index 95. Existing key zones remain valid. Mixed key/segment zones, multi-selection, zone editing, persistence and per-strip brightness use the existing zone system.
- Unassigned segments are off. Segmented zone effects override the strip's black base, retain the audio-zone priority convention and cannot affect keys or the eye. Switching away preserves assignments but disables their output and clears hidden segment selection. Existing overall opacity remains the last output stage.
- RhythmOrbit's display is replaced by Prism beat; its persistent enum token is unchanged. Broad mirrored colour facets fill every key, bass moves their scale, and bass events turn the facets and advance the palette with a coloured brightness punch. Frame-wide transforms and pulse strength are prepared once per frame, not per pixel. No thin rings or empty corners are required.
- The new Prism effect has no Lumo original reference; that tab is hidden and selection returns to the live canvas. Other Lumo references are unchanged.
- Research: audioMotion's official frequency-range/filter/mirror controls inspired the band-driven mirrored-field approach (https://audiomotion.app/docs/advanced.html). Butterchurn's official project was also reviewed (https://github.com/jberg/butterchurn). Prism is a local CPU reconstruction for low-resolution LEDs; no external visualizer runtime or preset code was imported.
- Tests cover rainbow meter geometry/brightness, saved palette, eye migration, all twelve unique segment indices, isolated endpoint colours, untouched 83 keys and eye, edit/reload, full-colour coverage and strong beat response after 83-key sampling. Offscreen UI checks confirm twelve real selectors and the reference-tab transition without hardware access.


### 2026-11-12 — simplified Prism palette

Prism beat retains its facet geometry, bass rotation/expansion, palette timing and brightness envelope. Colour now uses two dominant hues separated by 0.20 turns, with a short transition over facet values 0.4–0.6. The extra vertical hue modulation and per-column spectrum hue jitter are removed. Custom colours use the same simplified two-region gradient. Actual 83-key tests require at least 60 keys to share the two main hues (with both hues present), ignore unrelated spectrum changes at fixed bass, and retain the existing beat-response/coverage checks.


### 2026-11-12 — live preview only

Removed the entire prerecorded Lumo prototype tab, reference player/decoder, embedded reference-frame asset, tab selection state and obsolete localized labels. The realtime 655×240 canvas and its controls remain directly visible. SoundWave's caption now says that the two waves follow bass, support independent colours and flowing rainbow. Library/migration tests remain; the old asset-preservation test now verifies the reference asset is absent from the app bundle.


### 2026-11-12 — effect defaults, named compositions and pointer-only controls

- Built-in base/animation/music presets reset their current EffectSetting to factory values. Reactive effects, decorative effects and the overall envelope have independent reset controls; these do not erase zones or unrelated layers. Saved user compositions have a separate Restore saved composition command.
- Custom mode can name and Save the whole lighting composition. My lighting presets appears in the left library; clicking loads it in Custom mode. Save updates the active record (including renaming); Save as new creates a distinct ID and rejects duplicate names. Names are 1–48 characters, stored as text rather than filenames. Keyboard profile name and key count must match before listing/loading.
- LightingComposition is a non-recursive, source-generated JSON snapshot. It includes the base parameters, wave colours, key interaction, zones/strip segments, overall opacity, strip/eye modes and effects, music-analysis parameters, overlays, metrics and imported-animation metadata. Active FPS, selected audio target, favourites/history and the library itself are excluded. Deep copies prevent later edits from changing saved defaults. Loading batches controller changes into one final layer rebuild and preserves the active audio target.
- GIF/ATK resources used by a composition are cached as immutable content-addressed files under user-presets beside settings. Later GIF imports cannot overwrite the saved animation; identical content is reused. Old unreferenced blobs are retained until a future cleanup feature.
- Settings writes use a same-directory temporary file and atomic replacement with a shared save gate. Explicit save reports failure and rolls back the library update, keeping the prior saved record/file. Tests cover overwrite, save-as, reload, deep-copy isolation, profile isolation, resource preservation and failed-write cleanup.
- Space/Enter/arrows previously activated focused UI controls. Workbench buttons/toggles/sliders/selectors no longer take keyboard focus, and MainWindow/ZoneEditorWindow suppress non-text key activation on both key-down and key-up. Text editing, Tab/Escape and Alt+F4 remain available; the native key hook and keyboard interactions are unchanged. A headless reproduction confirmed the old Space palette toggle, then verified it no longer toggles, sliders cannot be nudged, text fields accept spaces, and mouse toggles still work.


### 2026-11-12 — zone colour overrides

Zone colour was correctly saved, but AddEffectLayers recreated region presets with their default Rainbow=true options. Shared zone construction now defaults to the explicit zone colour for key zones and segmented-strip zones, while global presets retain their own palette settings. Starlight and audio Spectrum zones also accept the selected colour; their global overlays retain the original palette. Explicit AudioZoneMode.SolidRainbow remains rainbow. Existing saved compositions need no migration because their zone colours were already persisted. Tests reproduce the previous failures and cover six pattern types, strip isolation, star colour, audio spectrum and global/explicit rainbow preservation.

### 2026-11-12 — independent region parameters and explicit composition entry

- Region effect adjustment opens an anchored, scrollable Flyout inside the workbench. It uses the current region editor and applies through the same Assign/Update action; no standalone editor window is launched. Rainbow can be explicitly restored after selecting a colour. Applicable speed, width, density, saturation, brightness, direction, mirrors and SoundWave dual colours are independent of the base preset. Reset changes the draft to factory settings; Apply commits it.
- ZoneSetting stores optional EffectSetting parameters, including key and segmented-strip zones; named compositions preserve them. Old zones without parameters retain their chosen single colour. Music regions use the existing selected capture source, expose capture controls, and retain temporal state across unrelated rebuilds; changing their mask replaces the cached layer.
- Entering Custom from a regular preset starts with that current preset and its parameters, clears old unsaved zones and disables old interaction/overlay/envelope layers. Explicitly loading a named composition still restores its full snapshot; an already active custom session remains editable. Leaving Custom sets both strip and eye to independent spatial rendering. Their Custom choices are disabled outside Custom with explanatory captions; decoration preview clicks cannot bypass this restriction.
- Verified by full rebuild, palette/render/brightness and settings/named-composition round trips, mode-entry tests, music-mask cache tests, and an Avalonia headless Flyout check (bindings, key suppression, Apply/close/re-edit and disabled decoration items). No physical device or foreground desktop interaction was used.

### 2026-11-12 — remove saved compositions

Each local My lighting presets row has a separate Delete button. Deletion is profile-scoped, persisted atomically, and serialized with saves/loads. Removing the active saved record clears its saved identity/name without altering the currently rendered composition; a subsequent Save creates a new record. An inactive deletion preserves the active record. Failed writes restore the original list and active identity and report failure in either mode. Shared immutable animation assets are retained. Tests cover active/inactive removal, restart persistence, failed-write rollback, saving again, and an offscreen pointer click without accidental preset loading.

### 2026-11-12 — preset consolidation and unused original UI cleanup

- Removed Stream, Trail, Windmill, SpiralRing and Nailong from the library/region selector and deleted their procedural implementations. MulticolorSineSectors is merged into SineSectors: Rainbow=true produces the former multicolour frame exactly (a pre-change RGB SHA256 fixture verifies this), while Rainbow=false uses the chosen colour. Factory defaults use the former pink with Rainbow=false. Fire's Traditional/Simplified Chinese label is now Fire rather than Full-board fire.
- App EffectChoice numeric slots/names remain only for saved-file compatibility. Old MulticolorSineSectors base settings, favourites, recents, ordinary zones, strip zones and named composition snapshots normalize to SineSectors and preserve explicit palette/geometry settings; missing legacy region parameters default to rainbow. Other retired presets follow the existing fallback policy. Named snapshots are copied before migration.
- Removed the original unused standalone ZoneEditorWindow and unreachable MainWindow handler, its unused title label, and the Scope-II-only ScancodeMap wrapper. The existing scan mapping tests now exercise ScancodeResolver directly. Active Scope II/Falchion profiles and shared rendering, device, input, GIF, settings and audio infrastructure remain because current features reference them. The separately requested pristine original preview is retained.
- Full Release rebuild and 652 tests pass. The count is lower because deleted preset cases no longer exist; saved-state migration, the merged rainbow reference and existing active effects remain covered. No desktop/game interaction or physical keyboard test was performed.
