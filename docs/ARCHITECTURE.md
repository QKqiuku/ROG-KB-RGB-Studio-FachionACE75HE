# Architecture

## The core idea: the keyboard is a 107‑pixel display

The keyboard's firmware effects are a fixed menu, and some don't even work on
this device (hardware Wave is dead). So instead of driving firmware modes, the
app renders **everything in software**:

1. A render loop ticks ~40×/sec.
2. It composites a stack of **effect layers** into a 107‑LED frame buffer.
3. It serialises that frame into the vendor HID **Direct** command and streams
   it to the keyboard.

This was validated by a proof of concept (`tools/ScopeIIProbe`) before any
production code: the device sustains ~42 FPS of Direct writes, which is the
enabler for software‑driven effects — including the wave the firmware can't do,
plus reactive, audio, and metric effects the firmware will never have.

## Layered compositor

A frame is built by a `Compositor` from an ordered list of `IEffectLayer`s:

- Each layer has a **generator** (`Render`), a **key mask** (which LEDs it
  applies to), a **blend mode** (Normal / Additive / Multiply / Max), and a
  **z‑order**.
- The compositor renders each layer (z ascending) into a scratch buffer, then
  blends it into the frame using the layer's mask + blend.

One abstraction expresses both "one global effect over all keys" (a single
layer, mask = all) and "different effects on different key groups" (multiple
masked layers — the Synapse‑style zone editor). The metric overlay and reactive
overlays are simply higher‑z layers that touch only their own keys.

`KeyboardController` assigns z‑orders from **fixed priority bands** so the stack
reads top‑down regardless of how many zones exist:

| z band | layer | notes |
| --- | --- | --- |
| 500 000 | System metrics | top — always visible |
| 30 000 | Reactive overlay | global keypress flare + ripple (Additive) |
| 20 000+ | Audio zones | zone spectrum/volume |
| 10 000+ | Other zones | static zone effects |
| 1 000 | Audio overlay | global spectrum or bars (Over) |
| 500 | Starlight overlay | twinkling stars over the base (Over) |
| 250 | Base brightness | Multiply dimming the base effect only |
| 0 | Base effect | main‑UI effect over every key |

Reactive, Starlight and the global Audio overlay are independent main‑UI toggles,
not effect modes. Reactive composites **Additive** (flares add light, idle frames
stay transparent); Starlight and Audio use **`Over`** (a non‑black source replaces
the base so the stars/spectrum/bars stay legible even on bright effects, while black
is transparent). Reactive outranks Audio outranks Starlight; all stay below the
metric bars.

**Base brightness** is itself a layer: a `Multiply` of a grey at z 250, masked to
all keys, so it dims only what is below it — the base effect at z 0. Everything
above (Starlight, Audio, Reactive, metrics) is composited afterwards and keeps its
full intensity. That is what lets a dim base sit under bright twinkling stars: turn
the base **Effect** down to, say, 20 % so the keycap legends stay faintly readable
in a dark room, and enable the Starlight overlay on top.

Effects in `Core/Effects/Layers`: Solid, Breathing, Rainbow, Wave (by column),
Starlight (twinkling stars; one per key, neighbours excluded, distance-weighted
spawning), Gif (baked `.kbanim` playback), KeypressFade, Ripple, TempIndicator,
AudioReactive (spectrum), AudioVolume (loudness), AudioBars (bass/mid/treble VU
bars on three key rows), MetricOverlay (CPU/Mem/GPU bars).

## Render loop & threading

Three threads:

- **UI** (Avalonia) — selection/status only; never touches the device.
- **Engine** (`RenderEngine.RunAsync`, one long‑running task) — drains queued key
  events, composites, and writes the frame. The synchronous Win32 device write is
  the natural throttle; a `Task.Delay` tops up to the target period.
- **Input** (`Win32KeyboardHook`, historical class name) — a dedicated thread with
  a message-only window and `GetMessage`/`DispatchMessage` pump. A keyboard Raw Input
  registration uses `RIDEV_INPUTSINK` so physical events arrive in the background,
  independently of foreground low-level-hook chains. Key events enter the engine
  through a lock-free `ConcurrentQueue`.

Effect changes are applied by swapping the compositor's layer set on the engine
thread (a volatile "pending layers" reference), so the compositor is only ever
mutated by one thread.

**Connection lifecycle.** `KeyboardController` runs a fourth background loop that
detects the keyboard, drives it, and reconnects after an unplug. It polls
`FindAsync` every ~2 s while disconnected; on success it opens the device and
starts the engine + hook. A failed frame write makes the engine log once and
exit, which the loop treats as a lost device — it tears the engine/hook/device
down and goes back to polling. The UI follows a `StateChanged` event. So a missing
keyboard at launch, or a mid‑session unplug, self‑heals without a restart or log
spam.

## Platform abstraction (CLAUDE.md rule 4)

`Core` defines interfaces and contains **no** OS‑specific code:

- `IKeyboardDevice` / `IKeyboardDeviceFactory` — open, write a frame, apply a
  firmware effect, read firmware info.
- `IKeyboardHook` — global key events.
- `ILogSink` — logging.
- `ISensorFeed` — the IPC sensor stream contract.

`Windows` is the only project that names native libraries, implementing those
interfaces with raw Win32:

- **HID:** `SetupAPI` enumeration filtered by usage page `0xFF00` + the `mi_01`
  interface, then `CreateFile`/`WriteFile`/`ReadFile`. (Raw `ReadFile` keeps the
  leading report‑id byte, unlike hidapi — read offsets are `+1`.)
- **Keyboard input:** `RegisterRawInputDevices` with `RIDEV_INPUTSINK`, a hidden
  message-only window and an `[UnmanagedCallersOnly]` window procedure. `WM_INPUT`
  packets are validated and physical Set-1/E0 scan codes are resolved through
  `Core/Input/ScancodeResolver`. The receiver does not suppress legacy input,
  consume hooks, inject input or translate keys into text. Stop/reconnect removes
  the registration, destroys the hidden window and joins its thread.

## AOT design (rule 5)

The app publishes as **Native AOT, trimmed, with zero IL/trim warnings**:

- All P/Invoke uses source‑generated `[LibraryImport]`; native structs are read
  via blittable layouts, not reflection marshalling.
- MVVM uses CommunityToolkit source generators; Avalonia uses **compiled XAML
  bindings** (`x:DataType`); settings use `System.Text.Json` source generation.

The reflection‑heavy sensor/audio libraries (and the native GIF decoder) can't be
AOT‑trimmed, so they live in **separate non‑AOT processes** — `SensorsHost` and
`GifBakeHost` — kept out of the main app entirely.

## Sensors: the IPC seam

`SensorsHost` is a small console process the app launches (with a parent‑PID
watchdog so it never orphans). It hosts pluggable providers:

- `SystemMetricsProvider` — CPU % (`GetSystemTimes`), memory % (`GlobalMemory
  StatusEx`), GPU % and GPU temperature (NVML). No admin required.
- `AudioProvider` — WASAPI loopback capture + FFT → 24 logarithmic frequency
  bands, with per‑band auto‑gain, an RMS silence gate, and fast‑attack /
  slow‑release temporal smoothing. The capture **self‑heals**: when Windows
  invalidates the render device (idle/sleep/hibernate wake, a USB sound card whose
  power is toggled, or a default‑device change) NAudio raises `RecordingStopped`;
  the provider goes dark and rebuilds the capture from its `Poll` watchdog with a
  short backoff, re‑reading the (possibly new) default device and sample rate — so
  audio reactivity returns on its own, with no app restart.

It serialises `SensorSample` JSON lines over a named pipe
(`\\.\pipe\RSS_II_RGB.sensors`). `Windows/NamedPipeSensorFeed` reads and
reconnects; `App/SensorService` pumps the samples into a thread‑safe
`Core/Sensors/SensorState` that the sensor effect layers read each frame.

```
SensorsHost (non-AOT)  --named pipe-->  App (AOT)  -->  SensorState  -->  effect layers  -->  frame
   NAudio, NVML, Win32                   NamedPipeSensorFeed
```

## GIF animation: the bake seam

Playing a GIF on the keys reuses the same "keep the heavy library out of the AOT
app" trick, but as a **one‑shot** bake rather than a stream. `GifBakeHost` is a
second non‑AOT console helper that uses **Magick.NET** to decode a GIF, crop it to
a user‑chosen rectangle, and downsample every frame to the keyboard grid
(`MatrixCols`×`MatrixRows` = 24×6). It writes a tiny dependency‑free binary,
`.kbanim` (`Core/Animation/KbAnim`: magic, grid, then per‑frame delay + RGB cells —
~432 bytes/frame), then exits.

The crop UI (`App/GifCropWindow`) is an Avalonia window that shows the GIF's first
frame with a selection box locked to the grid's 4:1 aspect, so the result is never
distorted. `App/GifImportService` drives the flow (file pick → crop → launch the
helper with a `GifBakeRequest` JSON, read back a `GifBakeResult`) and saves the
`.kbanim` under `%LOCALAPPDATA%\RSS_II_RGB\anims\`. At runtime `Core/Effects/Layers/
GifLayer` loads the baked frames once and, each tick, picks the frame for the
elapsed time (looping) and maps each grid cell to an LED by its `(Row, Col)` —
so the AOT app plays the animation with **no image library linked at all**.

```
GifBakeHost (non-AOT)  --.kbanim file-->  App (AOT)  -->  GifLayer  -->  frame
   Magick.NET: decode/crop/resize          KbAnim.Load (pure bytes)
```

## Persistence

`App/SettingsService` loads/saves `AppSettings` to
`%LOCALAPPDATA%\RSS_II_RGB\settings.json` via a source‑generated JSON context
(enums as readable strings). The controller restores the global effect, audio
sensitivity, zones, and metric overlay config on launch; every UI change saves.

## Per-device layout (`KeyboardProfile`)

Everything device-specific — LED count, matrix geometry, the ordered key table
(render order + key ids), and the scan-code → index map — lives in a
`Core/Layout/KeyboardProfile`. The render path is driven entirely by a profile
instance: the engine threads it through `EffectContext.Layout`, the device sizes
its frame buffer and Direct packets from it, and the hook resolves presses with a
per-profile `ScancodeResolver`. The Direct command itself (15 LEDs/packet,
65-byte report, `0xC0 0x81`) is shared across the whole ASUS TUF keyboard family,
so adding a keyboard is data: define its `KeyboardProfile` and map its PID to it
in the device factory. `ScopeIILayout.Profile` is the one concrete profile today
(used by both Scope II RX and the layout-identical NX).

## Reference

The Direct/HID protocol, key‑id ↔ name map, and the 6×24 matrix were ported from
the read‑only `vendor/openrgb` TUF‑keyboard driver and baked into
`Core/Layout/ScopeIILayout.cs` as committed data (no runtime dependency on
`vendor/`).

### Background/fullscreen reactive-input correction (2026-11-12)

The owner reported that ordinary lighting continued in fullscreen games while
key-triggered effects stopped and resumed after returning to windowed apps. The
old single WH_KEYBOARD_LL dependency was replaced with background Raw Input;
the specific game's interception/integrity behaviour was not independently
reproduced. Tests validate actual Windows INPUTSINK registration (without
NOLEGACY/NOHOTKEYS), native structure sizes, make/break/E0 mapping, rejected
non-keyboard/invalid packets, idempotent start/stop, restart, cancellation,
single-receiver ownership and registration cleanup. No synthetic keystrokes,
foreground-window manipulation or game/anti-cheat bypass was used. Actual
fullscreen-game behaviour remains for the owner to verify after restarting.

References: [Raw Input overview](https://learn.microsoft.com/en-us/windows/win32/inputdev/about-raw-input),
[RAWINPUTDEVICE flags](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawinputdevice),
[RAWKEYBOARD layout](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawkeyboard).

### VALORANT follow-up input diagnosis (2026-11-12)

The owner confirmed the same failure in windowed, borderless and fullscreen
VALORANT, while ordinary lighting continued. The running binary was verified as
falchion75-background-key-input. A separate, non-elevated, short-lived receiver
counted only packets/decoded events/errors, with foreground classified solely as
VALORANT or other. Known physical typing outside the game produced 45 valid
packets; game foreground produced none, with registration still active and no
read failures. A standard GetAsyncKeyState comparison also saw no state changes
in the game. No key identities/text, window titles, screenshots or game memory
were recorded. All diagnostic receivers were stopped afterward.

Read-only token queries showed that both VALORANT processes were elevated and
the lighting app was standard. This confirms a privilege mismatch, not yet that
matching elevation resolves it. The owner deferred the elevated comparison;
no UAC prompt was triggered. Workspace launchers Start-75HE-Admin.cmd and
Test-75HE-GameInput-Admin.cmd are ready for an explicit later test. The app
launcher requires the existing single-instance process to exit first. The
three-minute diagnostic writes aggregate counts to .research/input-probe-admin.log
and controls neither lighting nor game input. No game/security settings were
changed. Native receiver health counters expose totals only for diagnosis.

### VALORANT permission remedy confirmed (2026-11-12)

After the owner exited the standard instance and launched the same app using
Start-75HE-Admin.cmd, they confirmed that key-reactive effects worked again
inside VALORANT. Matching the game's elevated permissions is therefore a
user-verified remedy on this machine. The normal launcher retains standard
permissions; use the explicit administrator launcher when needed. No game,
anti-cheat or system-security settings were modified. Other protected games
and future game/security updates have not been validated.

### Default interactive elevation (2026-11-12)

At the owner's explicit request after verifying the VALORANT remedy, the apphost
manifest requests requireAdministrator (uiAccess=false). The ordinary workspace
launchers delegate to Start-75HE-Admin.cmd, which elevates the bundled dotnet host
explicitly so the portable .NET runtime is available. Repeated launches use the
existing application single-instance/show-window handling. An old standard
instance must exit before upgrading. UAC consent remains required; no security
settings or scheduled startup tasks are changed. Falchion75's existing automatic
startup restriction remains unchanged. The release directory retains only the
current package and one rollback package after publishing.
