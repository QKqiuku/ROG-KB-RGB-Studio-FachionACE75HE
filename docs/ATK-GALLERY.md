# ATK animation gallery preview

The workbench's **ATK gallery** button browses public ATK shared lighting scenes.
It supports popular/latest sorting, title search and filtered batch pagination, without
logging in or connecting an ATK keyboard. It makes only the public list query;
no upload, account, like, favourite or import-counter action is performed.

## Gallery policy (2026-11-12)

The public gallery now shows **only pure keyframe animations**: one or more keyframe effects and no animation modules or other effect types. Static, audio, procedural and mixed works are hidden even when the local renderer can play them. The gallery header explicitly states this restriction. Listing, preview and Apply enforce it; both popular/latest continue scanning all remote pages even when an entire page is hidden. Active-keyboard form-factor matching still applies. Existing imported/cached scenes retain their previous playback support; the renderer capabilities described below are broader than what this gallery now offers.

## Active-keyboard layout filtering

The gallery now lists only sources whose declared form factor matches the active
`KeyboardProfile.FormFactor`. Falchion75 declares 75%; Scope II declares full-size.
Every future ROG adapter must declare its own form factor. Unknown target layouts
fail closed instead of inheriting a global 75% assumption.

Source evidence is an explicit model/layout statement in the source device name
or title, or consistent declared source grid dimensions. Description text is a
fallback only when stronger evidence is absent. Different or conflicting source
layouts are hidden. Unlabelled works are also hidden: sparse coloured frame bounds
and ordinary brightness percentages do not establish keyboard layout. This can
exclude usable but unlabelled works rather than guess their compatibility.

Listing, preview and import share the same rule; fit/scaling cannot bypass it.
If the active profile changes while the gallery is open, applying is disabled and
the user must reopen it. Browsing now traverses all remote 20-item pages through
the server's reported total, deduplicates IDs and shows all matching sources in
one scrollable list. Hot/latest selects the server ranking; filtering preserves
that order. There is no local pagination or 100-item stopping point. Progress
shows checked/total/matching counts, and completion is labelled only after the
last remote page succeeds. The server is a live catalogue, not a snapshot API;
concurrent additions or ranking changes can still move items between requests.

Network errors, an unexpectedly empty page, the five-minute overall deadline,
the existing 10,000-page API bound, or the 64M-character retained-scene budget
leave available matches labelled incomplete with a retry hint. Closing cancels
requests. Unknown/conflicting layouts stay hidden; no coloured frame bounds are
used to infer layout compatibility.

This update preserves previously imported cached animations and all saved settings.
Matching 75% form factor narrows the source selection but does not guarantee that
every model's wide keys/navigation geometry is identical; retain the preview check.

Select a work to preview it on this keyboard's geometry. Previewing leaves the
running physical effect and saved settings alone. Choose **Keep grid positions**
or **Fit to keyboard**, then **Import and apply**. Titles and creator attribution
are retained, along with the ATK work ID. Different source layouts can produce
different spatial results; inspect the preview before applying.

## Supported scenes

Scene version 1 supports validated keyframes and a bounded subset of audio layers (see MUSIC-AUDIO.md):

- Sparse per-cell RGB frames; independent frame durations or per-frame delays.
- Multiple keyframe layers with independent speed, brightness and looping.
- Non-looping layers hold their final frame.
- The current ATK shared-scene path's default screen mixing is reproduced.
  Keyframe opacity zero hides a layer; positive opacity is used as a visibility
  gate by that upstream path, rather than ordinary alpha-over compositing.
- Source dimensions are used when provided, otherwise inferred from all coloured
  frame coordinates. Fit uses nearest-cell sampling; it does not translate ATK
  coordinates into ASUS HID addresses.

Supported audio includes amplitude_bars, stereo_spectrum and automatic-layout
AudioFrequency; keyframe/static mixed scenes are accepted. The upstream time-based
spectrum/pulse companions retain their actual semantics. Other procedural/game
modules and absolute AudioFrequency placement without device geometry remain
unsupported and cannot be applied. There is no partial import that silently drops modules.
Unsupported/malformed/oversized works leave the current animation intact.

## Local playback and existing settings

Accepted scene data is cached under `%LOCALAPPDATA%\RSS_II_RGB\anims\` and restored
at startup without a network request. ATK uses its own effect and cache; the GIF
cache, original GIF path and existing zones remain unchanged. Import initially
selects preset mode. Custom mode can choose ATK as its base effect and add layers.

Original scene timing is independent of the application's 15/30/45/60 FPS target.
The source's suggested frequency never overwrites the user's frame-rate setting.
Changing frame rate, brightness or zones retains the scene's playback origin;
a new render clock after reconnect resets it when needed.

## Boundaries and validation

Anonymous HTTPS requests disable cookies and redirects, have a 20-second overall
deadline and an 8 MiB response limit. JSON nesting is bounded. The decoder caps
8 effects, 4000 total frames, one million decoded LED samples and 16 × 32 source
coordinates, validating RGB values, source sizes and timing before application.

Full Release rebuild: zero warnings/errors. **202 tests passed (159 Core + 43 App)**,
including sparse frames, timing, looping/final hold, screen blending, grid fit,
playback continuity/reconnect clocks, malformed input, response size limits,
anonymous request contents, preview isolation and import/restart persistence.

The deployed window loaded the public catalogue, paged and searched it, rejected
unsupported work selection, previewed work 528, then imported it and sent it to
the connected keyboard at about 60 FPS. Original GIF, zones and FPS settings were
checked before/after. The owner subsequently confirmed that the imported animation
played normally on the physical keyboard. Layout-filter regression cases cover
other/unknown/conflicting sources, all-layer dimensions, misleading percentages,
active-profile matching and import guardrails.

## Source and compatibility limits

Investigated ATK HUB 3.2.32 on 2026-11-10/11:

- https://hub.atk.pro/
- https://api-hub.vxe.com/v1/share/config/list
- https://bpcdn.atkgear.com/hub-v3/production/3.2.32/static/demoMode.store-afQcByDx.js
- https://bpcdn.atkgear.com/hub-v3/production/3.2.32/static/SvgIcon-Onf-rYUY.js

These are the current web application's interfaces, not a guaranteed third-party
API. Network/server/format changes can affect gallery access. Imported JSON is
treated only as validated animation data; no remote script is executed or bundled.

## Falchion75 animation coordinate correction (2026-11-11)

The ROG internal effect grid is not the ATK RS7 6x15 animation grid. Matching
form factor does not make row/column addresses interchangeable. For this
specific target and source grid, the shared decoder now maps Enter to 3_13,
Z..Slash to 4_1..4_10, Right Shift to 4_11 and Space to 5_5. All other existing
keys retain their source cells. Both preserve and fit use the same correction
when the source and target dimensions are 6x15; other grid dimensions and
keyboard profiles retain their existing behavior. HID addresses, selections,
procedural effects, GIF mapping and source-layout eligibility are unchanged.

Two ROG positions do not have equivalent painted cells in these RS7 resources:
PgDn at 4_14 borrows 3_14, and the physical Copilot/right-Control position at
5_11 borrows 5_12. These are spatial fallbacks, not claims of identical key
semantics. They apply only if the original cell is absent. Explicit black is
preserved, as are unpainted areas and layer timing. Independently editing these
extra target positions would require a separate mapping/editor feature.

The shared decoder is used for gallery preview, import and cached-scene reload,
so existing imported JSON benefits after restarting the updated application.
Source evidence: official `rs7_v2.json` keymap indices and reserved entries in
`demoMode.store-afQcByDx.js`, plus observed coordinates of public works 15618,
7172 and 9229. The official JSON has inconsistent default key labels in several
rows; the correction uses its indices and observed animation coordinates,
not those default key labels. This is an RS7-family grid adapter, not a claim
that every 75% keyboard has identical geometry.

Sources:
- https://bpcdn.atkgear.com/hub-v3/production/3.2.32/keymaps/rs7_v2.json
- https://bpcdn.atkgear.com/hub-v3/production/3.2.32/static/demoMode.store-afQcByDx.js

Offline checks cover every target key with coordinate-coded colours, both
mapping modes, sparse layers and explicit-black versus missing-cell fallback.
Physical keyboard verification of this decoder correction is still pending.

## Current implementation boundary (2026-11-12 code audit)

The decoder allowlist is keyframe plus static, spectrum, pulse, amplitude_bars and stereo_spectrum effects, and visible AudioFrequency modules only with settings.autoLayout enabled. Unsupported enabled types reject the complete scene; disabled layers/modules are ignored. This is a local code audit, not a fresh survey of every live gallery work.

- Other ATK procedural types remain unimplemented. Previously captured official catalogue samples contain rainbow, breathing, wave, tide, reload_wave, fps_radar, lightning, matrix, snake, ripple and positional_gradient. A work rendered into supported keyframes can still play even when its title describes one of those effects. Matching names in the local built-in library do not implement ATK scene semantics.
- Other animation modules are not implemented; captured samples include Scanner, Circles, Line, Particles, Bars, Starfield, ColorWave and GradientFlow. Absolute-position AudioFrequency requires source canvas/device placement not supplied by these exported scenes. Automatic-layout audio sampling is approximate and does not recreate sub-key antialiasing.
- The gallery UI preview contains keys only; actual Falchion playback also supports derived strip/eye colours and local decoration overrides in Custom mode. Gallery imports preserve their scene data; there is no ATK frame/layer/module editor or source-geometry editor in this app.
- Account login, ATK cloud favourites/likes, uploads, publishing, original-work deletion and import counters are not integrated. Local named compositions are separate from those account features.
- Only declared matching form factors are offered. Unknown/conflicting layouts remain hidden, and identical form factors do not promise identical geometry. RS7-specific remapping/fallbacks apply only to the documented 6x15 source/target case; other ROG keyboards require their own verified profile and mapping.
