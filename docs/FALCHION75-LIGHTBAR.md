# M705 light-bar investigation

## Status

Implemented and owner-confirmed on M705 firmware 1.00.50. The main bar uses
12 addresses 17,25,...,105 in left-to-right order; the ROG eye is address 113.
The 83-key profile and its HID addresses remain unchanged. The device appends
13 decorative colours to each completed frame, using seven packets with counts
15/15/15/15/15/15/6. All seven matching acknowledgements must complete before
publishing the preview snapshot.

By default bar/eye colours mirror the final top-row source indices
0,1,2,4,5,6,7,8,9,11,12,13,14. Each region now has an independently saved mode:
Follow keys, Independent position, or a disabled Custom placeholder.
Decorative LEDs remain outside key selections and zone indices.
Preview shows the last acknowledged colours for each region, not optical readback.

## Evidence collected on 2026-11-11

- Local Gear Link 1.00.24 manifest identifies PID 7293 / 0x1C7D, M705.
- Its `rog-m705` layout declares 128 RGB slots and five side-group entries:
  Bar1=16, Bar2=40, Bar3=72, Bar4=96, Bar5=120.
- That same model advertises `sideLight:false`; a generic `setSideLightEffect`
  method elsewhere in the shared bundle is not evidence this model supports it.
- A transient `00 C0 81 05 00 [id,r,g,b]...` probe addressed those five entries.
  All exchanges returned matching `00 C0 82 05 00` acknowledgements.
- Owner photograph taken during the five-colour probe showed rainbow-like bar
  output rather than the requested red/green/blue/yellow/purple segments.
- Subsequent white/off probes also received matching acknowledgements, but the
  owner observed the top bar continuously black. The workbench was set to a
  reactive ripple effect, whose keys are normally black while idle; this does
  not establish that key control failed.
- Probe cleanup read back software mode 0. No firmware, calibration, actuation,
  key-binding or flash-save command was sent.

## Protocol distinction

The existing live transport uses software mode `74` and sparse `C0 81` entries.
The downloaded official advanced-animation service instead uploads sequential
RGB frames through `C1 00` (frame count/dimensions), `C1 01` (frame chunks),
`C1 02` (reactive settings), `C1 03` (brightness), and `C1 04` (apply/checksum).
Those commands do not establish a reversible live-control path for the bar;
none have been sent by this investigation.

The shared bundle also contains `12 02` / `51 0A` RGB-indicator mode query/set
(normal versus power) and `25 1E` / `51 2E` side-light query/set. Their relationship
to the M705 top bar remains unverified. Do not switch them speculatively.

## Earlier prepared diagnostic (subsequently completed below)

The standalone `.research/BarProbe.cs` in the parent workspace now prepares a
bounded RGB-slot test over descriptor indices 0..127 only, using the existing
transient Direct command. It keeps A/address13 red and D/address29 blue as
positive controls. Other slots alternate white/off in four ten-second phases,
with each packet acknowledged and restoration in `finally`.

- If A/D work and the bar alternates: locate the actual contributing slots.
- If A/D work and the bar stays dark: the tested slot range/path did not control
  it in that state; this alone does not prove permanent lack of support.
- If A/D do not work: investigate the diagnostic/transport first.

Do not run while the owner is gaming. Do not stop/restart the running workbench,
request UI focus, or send device commands until an observation session resumes.
Future testing must preserve the current settings and confirm mode restoration.

## Sources

- Official manifest: https://gearlink.asus.com/view/7293/manifest.json
- Locally downloaded official layout: `.research/75he-base.js` (parent workspace).
- Locally downloaded official service: `.research/75he-protocol.js`.
- OpenRGB sparse Direct transport reference:
  https://github.com/CalcProgrammer1/OpenRGB/blob/master/Controllers/AsusAuraUSBController/AsusAuraKeyboardController/AsusAuraKeyboardController.cpp

Official layout identifiers are candidates, not hardware-confirmed mappings.

## Subsequent physical evidence

The full 0..127 transient RGB test completed with per-packet acknowledgements;
the owner confirmed A red/D blue and the ENTIRE bar alternating white/off.
Thus this Direct path can control the bar; the original five addresses were
insufficient. A reserved-slot grouping (indices modulo 8: 0=red, 1=blue,
others=green, known keys excluded) produced an entirely BLUE bar. This narrows
bar control to candidates 1,9,17,...,121, not the official modulo-0 indices.

Testing only 17,41,73,97,121 while clearing all other slots produced the owner
report red-black-green-black-blue-black-yellow-black-purple-black. Those five
addresses are responsive but do not cover the bar. Do not implement a five-zone
adapter from them. The sixteen modulo-1 candidates are now under grouped-order
calibration; exact physical coverage and order are still being verified.

All completed probes restored and queried software mode 0. The diagnostic no
longer launches or focuses any workbench after completion; the owner restarts
the current version manually. Hardware tests are now explicitly owner-observed.

## Final production-device verification

The owner confirmed the repeated-pattern order starts blue/yellow/red/green
and ends with a blue ROG eye, isolating candidates 17..113 in steps of 8.
The endpoint probe then showed blue at the left, a long white middle, yellow
near the right and purple on the eye; candidates 1,9,121 had no observed bar
positions. Unobserved candidates are excluded from production writes.

The actual Falchion75Device and Win32 factory then sent a static pattern with
strip left red/middle white/right blue and eye green for 30 seconds, followed
by a 20-second rainbow wave. The owner confirmed both bar/eye pattern and wave.
Several top-row keys intentionally carried those same colours to supply the
mirrored source pixels; this was not accidental HID address aliasing.
All 322 seven-packet wave frames were acknowledged (16.08 FPS in a basic
20-FPS Task.Delay probe). This is not a benchmark of the workbench's 60-FPS
engine. Disposal confirmed software mode 0. No UI automation was used.

Full rebuild: zero warnings/errors. Tests: 293 Core + 63 App = 356 passing.
They verify all original 83 key payloads remain unchanged, exact appended
bar/eye address and colour order, seven per-packet acknowledgements, mode
restoration, preview mirroring and clearing without adding selectable keys.

## Independently saved modes (2026-11-11)

`LightBarMode` and `EyeMode` are stored separately in the existing model settings.
Old settings default to FollowKeys; Custom and invalid numeric modes normalize
to FollowKeys until custom editing is implemented. Controls are below the
keyboard preview and do not take keyboard focus.

- FollowKeys copies final composited top-row pixels, including key-zone overrides.
- Spatial computes the current global effect/parameters and global reactive
  overlays at an extra row above the keyboard (row -1). Main bar and eye can use
  different modes. These are logical matrix positions, not millimetre calibration.
- GIF samples the raw animation's upper row. ATK retains raw upper-edge samples
  during scene parsing, including fitted scenes where top-row keys sample a
  different source row. Original frame durations and layer mixing are preserved.
- Key-only zones/metrics remain key-only; there are no selectable decorative keys.
  Independent spatial layers are separate instances, including retained key-dye
  state, so they cannot mutate the original 83-key render or duplicate its events.

The engine builds the 83 original key pixels normally and decorates the frame
with 13 follow/spatial pixels. A single immutable 96-pixel snapshot is published
only after all seven packets complete. The key-only LastSentFrame API still
returns 83 pixels. Pending layers and modes are exchanged atomically.

Default FollowKeys avoids the extra effect pass. Spatial adds a separate
96-position effect pass; it does not add HID packets or alter target FPS.
The original measured addresses and mode restoration protocol are unchanged.
A CountdownFireworks guard now excludes negative rows from its digit bitmap.

Full Release rebuild: zero warnings/errors, 370 passing tests (305 Core + 65 App).
Checks cover four mixed modes, unchanged key pixels, all procedural and key
interaction effects above the board, raw ATK/GIF edge sampling, fitted ATK,
explicit decorative HID payloads, acknowledgement-gated atomic previews,
independent settings restoration and unavailable-mode fallback.
A read-only review identified and verified fixes for lost pending updates and
incorrect fitted-ATK edge sampling. This mode-selection UI and spatial behavior
have not yet been visually verified on the owner's hardware. No keyboard tests
or UI automation were run while implementing this setting change.
