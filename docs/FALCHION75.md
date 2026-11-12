# Experimental Falchion Ace 75 HE adapter

Status: basic wired lighting verified on one M705, firmware `1.00.50`.
Explicit opt-in remains required. This is not a claim of compatibility with all
firmware revisions or of exhaustive per-key placement testing.

## Implemented behaviour

- Discover only ASUS `0B05:1C7D`, control interface `MI_01`, usage `FF00:0001`,
  with 65-byte input and output reports. Normal app launch excludes this model.
- 83-key ANSI layout; single Space LED. The top 12-segment bar and ROG eye independently select final top-row following or spatial effects; other status LEDs remain excluded. See [light-bar evidence](FALCHION75-LIGHTBAR.md).
- Existing zone editor, software effects, scan-code reactive input, audio/metric
  effects and GIF crop/bake use the selected keyboard layout. Scope II numeric
  addresses are translated by semantic key name rather than reused on M705.
- Main lighting workbench: physically proportioned keys, click/multiple/drag
  selection, HSV picker, six-digit RGB input validation, zone creation and
  editing in place, with automatically saved settings. Advanced controls are
  collapsed below. Every key's fill follows the final successfully sent RGB
  frame, including all effects, brightness and overlays. Selected keys retain
  their live fill and gain an outline. Preview is updated on the UI thread,
  does not queue render callbacks, and stops polling frames while hidden.
  Immutable snapshots are published only after all seven packet acknowledgements;
  incomplete writes are not displayed as complete frames. A stopped engine
  clears the preview and shows Not synced. There is no per-LED colour readback.
  The brightness slider controls only the base effect, as in the compositor.
  Simplified Chinese, Traditional Chinese and English follow the Windows
  display language.
- Enter software mode before streaming. Send each `C0 81` packet only after
  its predecessor's matching `C0 82 <count> 00` acknowledgement. A report has
  a 1.8-second deadline; wrong/unrelated replies do not advance the frame.
- Read firmware with the verified `12 00` query. No legacy blocking layout query.
- Restore software mode to off and read it back when the owned connection is
  disposed, including after cancellation/failure. An already-active software
  mode is left alone and reported as a conflict with another controller.
- Normal tray Exit waits for keyboard restoration before closing the app.
  OS shutdown allows up to five seconds for cleanup. Forced process termination
  or power loss can interrupt restoration; reconnect/Gear Link may be needed.
- Separate `settings-falchion75.json` and GIF output. Experimental auto-start
  is disabled. No flash, preset, firmware, calibration, binding, actuation or RT
  writes are exposed by this device implementation.

Default target: **60 FPS**, with **15 / 30 / 45 / 60 FPS** tabs in the title bar.
Changes apply to the running engine without reconnecting or restarting the
animation. The global target is saved independently of preset parameters and
retained across preset/custom mode switches. Saved targets are mapped to the
nearest available choice (40 → 45, 120 → 60), with a maximum of 60 FPS.
A new production-path sine-wave test completed
600 full frames in 10.0067 seconds (**59.96 FPS**), including every packet
acknowledgement. Unpaced transport measured 165.41 full frames/s over three
seconds. These are short-run results on this machine/firmware, not guarantees
for every workload or firmware.

The earlier 16.01 FPS result used a target of 20 and coarse Windows delays;
it was not the hardware ceiling. At target 60, coarse pacing produced about
40 FPS. A scoped 1 ms Windows timer request plus absolute frame deadlines
removed that scheduling limit. Precision is released when the engine stops,
including on cancellation or a device failure. Windows can reduce precision
for hidden/minimized apps, and higher precision can increase power use:
https://learn.microsoft.com/windows/win32/api/timeapi/nf-timeapi-timebeginperiod

## Address sources and hardware corrections

The base `rog-m705` LED-index table comes from ASUS Gear Link 1.00.24:

- https://gearlink.asus.com/view/7293/manifest.json
- https://gearlink.asus.com/view/7293/chunk-1.00.24-7293-1787215788-772aaa-TZdLhL9S.js
- Protocol/mode module:
  https://gearlink.asus.com/view/7293/chunk-1.00.24-7293-1787215788-772aaa-DFtwurI9.js

The reference fixture preserves that public table. The first three corrections
were located with seven binary lighting groups and an owner-confirmed
three-colour check. A later owner report identified Enter and Up still retaining
hardware colours. Their adjacent-column candidates were tested for 20 seconds
with acknowledged packets; the owner confirmed Up red and Enter blue. A further
20-second probe confirmed Right Shift red, Up green and Enter blue together.
Right Shift uses the old Up address; moving Up to its correct address exposed
Right Shift's own missing correction. A subsequent Slash report led to a
self-paced five-group check of the entire lower letter row and its neighbours.
The owner individually confirmed all 15 named keys in red/green/blue groups:
Z/X/C, V/B/N, M/Comma/Period, Slash/Right Shift/Up, Left Shift/Page Down/Enter.
The result recorded five successful groups and confirmed software-mode
restoration. Z through Slash were all one address column behind; correcting
only the wide right-side keys had left that original row error in place.
All sixteen corrected addresses are:

| Key | Gear Link index | Observed Direct address |
| --- | ---: | ---: |
| Space | 31 | **55 / 0x37** |
| Page Up | 109 | **117 / 0x75** |
| Page Down | 110 | **118 / 0x76** |
| Enter | 101 | **109 / 0x6D** |
| Up arrow | 102 | **110 / 0x6E** |
| Right Shift | 94 | **102 / 0x66** |
| Z | 14 | **22 / 0x16** |
| X | 22 | **30 / 0x1E** |
| C | 30 | **38 / 0x26** |
| V | 38 | **46 / 0x2E** |
| B | 46 | **54 / 0x36** |
| N | 54 | **62 / 0x3E** |
| M | 62 | **70 / 0x46** |
| Comma | 70 | **78 / 0x4E** |
| Period | 78 | **86 / 0x56** |
| Slash | 86 | **94 / 0x5E** |

All render indices and geometry are unchanged, including Enter 57, Z through
Slash 60–69, Right Shift 70 and Up 71, so saved key selections,
scan codes and visual geometry remain compatible. The address probe restored
software mode 0 and read it back after the test.

A (`13 / 0x0D`) red and D (`29 / 0x1D`) blue were separately owner-confirmed.
A complete all-off frame with only A/D coloured and the subsequent rainbow
wave were then confirmed correct by the owner. This checks coverage, these
specific positions and animation; it does not independently label every key.
The physical Copilot-cap position is named RIGHT_CONTROL for reactive mapping;
its actual events depend on the Gear Link binding. Fn is not a normal Windows
scan-code trigger.

The descriptor's 128 slots include unused/decorative positions; the app emits
83 known key entries plus 13 measured bar/eye entries, not all 128 slots. Related family transport:
https://github.com/CalcProgrammer1/OpenRGB/blob/master/Controllers/AsusAuraUSBController/AsusAuraKeyboardController/AsusAuraKeyboardController.cpp

## Hardware test record: 2026-11-10

1. Read-only `12 00` returned firmware `1.00.50`, six profiles, current effect 4,
   current profile 6. `27 00` returned software mode 0.
2. Enabling mode with `74 00 ... 01` and restoring `... 00` both worked.
3. Six packets sent without awaiting acknowledgements failed visually: some
   keys went dark, others retained frozen colours. This path is not used.
4. One A/D packet replied `00 C0 82 02 00 ...`; the owner confirmed A red/D blue.
5. Awaiting all six packet acknowledgements improved coverage. Space and both
   Page keys retained colours; the ROG bar was deliberately untouched.
6. The interactive 0..127 RGB-index grouping identified the three corrections
   above. The owner checked Space red, Page Up green, Page Down blue. Software
   mode restoration was read back as 0.
7. The actual `Win32KeyboardDeviceFactory` and `Falchion75Device` then drove the
   corrected static frame for 20 seconds and a rainbow wave for ten seconds.
   All report exchanges succeeded, measured 16.01 FPS, and disposal read back
   software mode 0. The owner confirmed static, wave and original-effect recovery.

No reset, flash, firmware, calibration or input-setting command was sent.

## Build and run

Use the .NET 10 SDK and the main README build instructions. The local preview
in `dist/falchion75` is framework-dependent Release, **not Native AOT**; it needs
the .NET 10 x64 runtime. Keep the complete directory, including `sensorshost`
and `gifbakehost`.

Double-click `Start-Studio.cmd`, or run:

```powershell
.\RSS_II_RGB.App.exe --experimental-falchion75
```

Exit an existing RSS_II_RGB tray instance first. Close other lighting
controllers/Gear Link device tabs. Use the tray's Exit to stop the app and
restore hardware lighting, or use the workbench's **Exit and restore** button;
the window's X only hides it to the tray.

## Validation and remaining limits

Automated tests cover all reference indices plus measured corrections,
model gating, geometry, scan-code/audio/metric resolution, packet contents,
per-packet acknowledgement matching, ignored unrelated replies, deadlines,
cancellation, failed mode entry, restoration, forbidden writes and shutdown
ordering/idempotence. App tests additionally cover selection gating, invalid
RGB input, HSV/hex synchronization, saved-zone reload/update/removal, physical
key geometry and Windows locale selection.

The actual connected workbench was checked for click/drag selection, saved
zone reload and update without duplication, invalid-colour rejection,
HSV/hex interaction, advanced-panel expansion and Exit and restore. After
normal exit, a read-only `27 00` query confirmed software mode 0. Test zones
were removed by restoring the pre-test settings before delivering the build.
Full Release rebuild completed with zero warnings/errors; all **202 tests**
passed (159 Core + 43 App). New tests verify post-write publication, immutable
snapshots across ticks, final composited colours, failure gating and per-key
preview without changing selection or saving configuration. Additional tests
cover all new animated presets, single-colour wave motion, parameters, masked
reactive input, GIF playback speed, preset isolation, legacy-settings migration,
per-effect persistence, custom-base restoration and timer-lease cleanup.
Countdown animation starts at 3 on selection regardless of engine uptime;
empty colour input is rejected without throwing.
The optional [ATK gallery](ATK-GALLERY.md) imports validated public keyframe
scenes, preserving GIFs, zones and the global frame-rate choice. Audio/procedural
modules remain unsupported; previewing is separate from physical application.
Address regression cases include all 15 physically labelled lower-row/neighbour
keys, checking fixed selection indices, scan codes and expected Direct addresses
with coloured packet payloads, plus literal Enter/Up/Right Shift packet slots.
These tests first failed for the ten incorrect Z–Slash addresses before the
shared layout was corrected. A running
engine pacing test verifies that changing the target changes frame intervals
without completing/restarting the loop; app tests cover target clamping,
settings persistence and preservation across mode switches.

The light workbench separates preset-only and custom-layer modes. Selecting a
preset pauses zones/audio/metrics/reactive overlays without deleting them;
Custom restores the previous base and saved layers. Effect parameters are
stored independently. Algorithm sources and implementation scope are recorded
in [WORKBENCH-LIGHT-DESIGN.md](WORKBENCH-LIGHT-DESIGN.md).

Still untested: individual colour labels on all 83 keys, actual app reactive/
audio/GIF editing flows, long-duration streaming, unplug/reconnect, sleep/wake,
other firmware revisions and forced shutdown recovery. Other status LEDs and
custom decorative-layer editing are not implemented. Keep this model opt-in until that broader validation exists.
