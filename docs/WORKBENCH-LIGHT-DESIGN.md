# Light workbench update

Approved request: match the supplied light-grey reference, isolate presets from
custom layers, extend the referenced effects and expose meaningful parameters.
Keep the existing actual-sent-frame preview and all saved zones.

- Keyboard above the controls; left preset pills; right speed/brightness and
  colour wheel. Custom mode additionally exposes base selection, zone editing
  and the existing advanced overlays. A preset disables those layers without
  deleting their configuration. Old settings start in custom mode.
- Regular presets: Solid, Breathing, Rainbow, Key trigger, Rain, Ripple, Stars,
  Stream, Wave, Trail, Sine wave, Windmill, Waterfall, Bloom and Off.
- Procedural animations: Brick breaker, Countdown fireworks, Colour vortex.
  Existing GIF import remains available separately; these animations need no
  downloaded binary assets.
- Parameters are saved per effect: speed, brightness, direction, width,
  density, colour mode, saturation and primary colour, shown where applicable.
- Target 60 FPS by default, with 15 / 30 / 45 / 60 FPS tabs capped at 60;
  changes apply live and persist. Legacy values use the nearest offered rate.
  Actual completed-frame rate is shown separately. Verified
  600 acknowledged frames / 10.0067 s = 59.96 FPS after scoped 1 ms Windows
  precision and deadline pacing. The old ~16 FPS result was paced, not a link
  ceiling. Unpaced output measured 165.41 FPS. Keep the verified packet protocol
  and restoration; no speculative firmware commands.

Algorithm references, consulted rather than copied:

- https://github.com/DLdota/OpenRGBEffectsPlugin/blob/master/Effects/Wavy/Wavy.cpp
- https://github.com/DLdota/OpenRGBEffectsPlugin/blob/master/Effects/Rain/Rain.cpp
- https://github.com/DLdota/OpenRGBEffectsPlugin/blob/master/Effects/RotatingBeam/RotatingBeam.cpp
- https://github.com/DLdota/OpenRGBEffectsPlugin/blob/master/Effects/Bloom/Bloom.cpp
- https://github.com/CalcProgrammer1/OpenRGB/blob/master/Controllers/AsusAuraUSBController/AsusAuraKeyboardController/AsusAuraKeyboardController.cpp

Implement independent time-based C# patterns in the current layer renderer,
not the unrelated keyboards' firmware protocols. Test animation/input timing,
parameter influence, preset isolation, migration and persistence; then inspect
the actual light UI, measure target-60 throughput and publish after checks pass.

## Dark ROG presentation (2026-11-11)

The workbench uses a near-black background, charcoal panels, muted borders and
red Fluent accents. The existing layout, independent preset/parameter scroll
areas, input-focus rules and dynamic RGB brushes remain unchanged. The gallery
and editor use the same dark application theme.

The final decorative preview slot now uses the ROG eye silhouette instead of a
rectangular segment. Its fill remains bound to that slot's sent RGB brush;
a dark outline keeps the eye recognizable while off. The title uses a fixed
red copy. Three native vector paths were extracted from the official header SVG:
https://rog.asus.com/rog/nuxtStatic/img/logo-rog.svg
No SVG runtime dependency or remote asset loading was introduced.

Visual verification used a separate Avalonia Headless + Skia harness under the
parent workspace `.research/RogThemePreview`; its test-only dependency is not
referenced by the shipped app. It rendered the actual window, checked eye
geometry/RGB binding and produced `.research/rog-theme-preview.png` using
synthetic colours, without opening a desktop window, starting device discovery
or installing keyboard hooks. Release rebuild passed with zero warnings/errors;
all 370 existing tests passed. Desktop DPI/hover appearance still needs owner
observation. No live keyboard lighting test was run for this presentation change.
