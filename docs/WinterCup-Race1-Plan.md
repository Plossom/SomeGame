# Winter Cup · Race 1 — "Snowball Village" — build plan

Goal: the first race of the Winter Cup. Easy to finish (it is race 1 of a new cup), but never boring:
flowing bends instead of straights, several ways round, and a surprise around every corner — in the
spirit of Mario Kart. It must look clearly *better* than the Valley races: new snowy ground, road,
kerbs, scenery and moving life.

Keep: the car, controls (point-to-steer, hop, drift), HUD, combos, star rule (1★ = 6th rival,
2★ = 3rd rival, 3★ = fastest rival), loading screen, info window. Everything new is data-driven in
`TrackLayout`, so later Winter races reuse it.

---

## 1. The lap (about 420 m, 3 laps, ~70 s)

```
                 ▲ finish/start in the village square
   ╭──────────── Village main street ─────────────╮
   │   ⇡ BOUNCE PAD → ROOFTOP RUN over 3 roofs ⇢   │   (A) roofs: shorter, a hop off the last roof
   ╰──────────────╮                    ╭───────────╯
                  │ gentle S-bends      │
        ╭─────────╯  past the snowmen   ╰──────╮
        │                                      │
   ICE-WALL HAIRPIN  ◀── (B) wall ride on the banked snow wall (fast line)
        │                                      │
        ╰──╮      FROZEN LAKE                  │
           │  road round the shore ───────╮    │   (C) straight across the ice: shorter, slippery
           │  ICE SHORTCUT (slides!) ════╪    │
           ╰─────────────────────────────╯    │
                       SKI JUMP over the creek ─╯   (D) big air, boost arches before it
```

Surprises along the lap
- Penguins waddle across the road in a little line (bump one → small spin).
- Snowballs roll down the slope across the road (like the logs, but growing as they roll).
- Ice-crystal boost arches: drive through → short boost.
- Snowmen by the road get knocked over (puff of snow, no penalty) — pure fun.
- Lap 2/3: the village lights are on and a sled train passes behind the start (scenery life).

Easy, not straight: wide road (8.5), soft S-bends, one hairpin with a forgiving wall, no water to fall
in except the creek under the ski jump (with a generous landing).

## 2. New systems (engine)

| # | System | What | Touches |
|---|--------|------|---------|
| S1 | **Biome** | `TrackLayout.biome` (Meadow / Snow): the race swaps ground, road, kerb, run-off materials, camera colour, sky life (snowfall instead of birds), tyre tracks (deep in snow), engine smoke tint | TrackRenderer, SkyLife, Race scene materials, ArtGenerator |
| S2 | **Branch routes** | `TrackLayout.branches`: extra road pieces (polyline + width) that leave the main road at lap distance A and rejoin at B. Driving on one counts as road; progress maps branch position → main distance (A..B), so checkpoints/positions keep working | Track, TrackSensor, RaceProgress, TrackRenderer, TrackAheadView |
| S3 | **Surfaces** | Zones with their own grip/speed: *ice* (low grip, slides, keeps speed), *deep snow* (off-road) | TrackLayout, Track, CarMovement |
| S4 | **Wall ride** | `banks`: a banked strip on the outside of a corner. On it the car tilts with the wall (sprite skew + shadow offset), keeps full speed if fast, slides down if slow; leaving it at speed gives a boost | Track, CarMovement, car visuals |
| S5 | **Rooftops** | A branch of kind *Roof*: drawn as snowy roof tiles above the ground (shadow), entered by a bounce pad that lands on its start, left by a ramp lip at its end (car flies down to the road). Driving off a roof edge = drop with a hop onto the street | Track, CarTerrain, TrackFeatures |
| S6 | **Life & hazards** | Penguins (crossing walkers), snowballs (rolling sweepers that grow), boost arches (drive-through boost), knock-over snowmen | TrackHazards, CarTerrain, art |
| S7 | **AI** | Rivals choose routes by personality (some always take roofs/ice, some never), ride the wall, use arches | AIDriverInput |
| S8 | **Combos** | New tricks: ROOFTOP!, WALL RIDE!, ICE SLIDE!, ARCH BOOST! | ComboDisplay |

## 3. New art (procedural, Alpine Rally style, winter palette)

- Ground: snow with soft blue drifts and sparkles; ice (pale blue, cracks, glints).
- Road: dark asphalt with snow dust at the edges; kerbs as white/blue snow banks; run-off = packed snow.
- Roofs (top-down): snow-covered roof tiles, chimneys with smoke, ridge line.
- Scenery atlas (winter): snowy pines (top-down stars with white tips), frosted bushes, log cabins
  with snowy roofs, igloo, snowmen, ice crystals, sled, ski-lift pylons, gift boxes, lamp posts.
- Moving: penguins (2-frame waddle), snowballs (rolling, growing), boost arches (glowing ice), snow puffs.
- Track preview picture, map stop on the winter road, info text.

## 4. Workflow (each phase ends playable, tested with screenshots, committed)

1. **P1 Biome + art base** — snow ground/road/kerbs/run-off/scenery atlas; `biome` switch; winter
   catalog + level skeleton (flowing layout, no gimmicks yet); appears on the winter map as race 1.
   ✔ drive a lap on autopilot, screenshots.
2. **P2 Surfaces** — ice zone (frozen lake shortcut), deep snow off-road. ✔ grip feels slippery, AI ok.
3. **P3 Branch routes** — generic branches + progress mapping + rendering + track view. ✔ a lap via
   the branch counts, positions stay right.
4. **P4 Rooftops** — roof branch art, bounce-pad entry, ramp exit, edge drops. ✔ autopilot rooftop lap.
5. **P5 Wall ride** — banked hairpin: tilt visuals, speed rules, boost on exit. ✔ fast line works.
6. **P6 Surprises** — penguins, snowballs, boost arches, knock-over snowmen, ski jump.
7. **P7 AI + combos** — route personalities, new tricks in the combo display.
8. **P8 Polish + balance** — preview, info text, 3 laps of all rivals measured → star times by the
   rule, map stop, loading screen, last screenshot pass. Phone build only when asked.

Guard rails: before every Editor step that enters/stops play mode or rebuilds scenes, check play mode
is "stopped"; never touch the player's saves without restoring them; no iOS build unless asked.
