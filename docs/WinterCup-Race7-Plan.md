# Winter Cup · Race 7 — "Glacier Spine" — design plan

Replaces Spiral Peak. LevelDefinition stays `Winter07` (saves are keyed by it); the track asset
`PenguinBay.asset` is rebuilt from scratch (new waypoints, new everything).

Goal: the second-to-last race and the first real **mountaineering** race. You climb a glacier,
cross the razor-thin ridge between two summits, and fly down a giant ski-flying hill to the finish.
It must be clearly harder than races 1–6: narrow road with open edges, things that break under you,
a bridge that moves, and one jump where *you* have to aim the landing. Every danger is telegraphed,
and a fall carries you back just before it (like the chasms today), so it costs seconds, not the race.

One lap, about **2,800 m**, ~2:40 for a good run. No northern lights, no glowing lamps.

---

## 1. The line (top-down sketch, finish is far from the start: a journey, `finishAtLastLine`)

```
                                   ▲ N
   SUMMIT B ●━━━━━━ (D) SWAYING ROPE BRIDGES ━━━━━━● SUMMIT A (ice tower)
   ski-flying  ╲      over the bergschrund          ╱  ╲
   hill tower   ╲                                  ╱    ╲ (C) THE SPINE: knife-edge ridge,
                 ╲  (E) SKI FLYING ⤵ big hill      ╱      ╲   collapsing cornices
                  ╲    aim the landing strip     ╱        ╲
                   ╲                             (B) SERAC ICEFALL: cracking thin ice,
     outrun ═══════╯                                 blue seracs, crevasse ladders
     FINISH in the                                       │
     valley stadium      ╭───── (A) GLACIER APPROACH ────╯
                         │   long sweeping climb up the tongue of the glacier
                 START ──╯   (wide, fast, warm-up)
```

Distances are first targets; they get tuned when the road exists.

| # | Section | Lap m | Width | What happens |
|---|---------|-------|-------|--------------|
| A | **Glacier approach** | 0–520 | 10 → 8 | Big sweeping S-bends climbing the glacier tongue, moraine rocks, a rope team of climbers walking beside the road. Boost pad 1 at ~180. Warm-up, but already a few open chasm edges on the outside of the bends. |
| B | **Serac icefall** | 520–1,050 | 8 → 7 | Weaving between huge blue serac blocks. **Thin-ice fields** (new mechanic 1) in the gaps between seracs; aluminium ladders over two crevasses (open edges, 6.5 m). Boost pad 2 at ~600 (on solid ground, before the first field). |
| C | **The Spine** | 1,050–1,650 | 6.5 → 6 | The knife-edge ridge from summit A along the crest: sheer drops both sides (chasms right up to the kerb), **collapsing cornices** (new mechanic 2) on the outside of three bends. Summit A: ice tower and summit cross, camera reveal of the whole route ahead. |
| D | **Rope bridges** | 1,650–2,050 | 6 | Three **swaying suspension bridges** (new mechanic 3) over the bergschrund, from rock pillar to rock pillar. Short solid rock islands between them. Boost pad 3 at ~2,000 on summit B (well after the bridges, well before the jump). |
| E | **Ski flying** | 2,050–2,500 | 7 → 30 | The in-run of a giant ski-flying hill (judges' tower, flag line, stands far below). Big take-off → **ski flying** (new mechanic 4): long hang time, you steer in the air onto the narrow K-line landing strip. Miss it = down the cliff, carried back to the in-run. |
| F | **Outrun & finish** | 2,500–2,800 | 12 | Wide curved outrun into the valley stadium, crowd and flags, FINISH. Boost pad 4 at ~2,560 (after the landing). |

Difficulty curve: A easy-medium → B medium (learn the thin ice) → C hard (narrowest, cornices) → D hard
(moving road) → E climax (one big aimed jump) → F breathe and finish.

## 2. New mechanics (none of these exist yet)

### M1 · Thin ice (`thinIce`: polygons on the road)
- **How it works:** Pale, glassy blue patches with a fine crack pattern. Each patch has a load: driving on it
  at **speed below ~60 %**, or **drifting** on it (sliding sideways), adds load. Driving straight and fast adds
  almost none. Too much load → the patch shatters under the car → splash into the dark water → carried
  back to before the field (as chasms do). The patch refreezes after ~4 s.
- **Telegraph:** Cracks spread visibly from the car as load builds (white spiderweb under the tyres), a
  rising creak sound, the patch flashes white just before it breaks. Patches broken by a rival stay
  open dark water for a few seconds — a visible hole you must steer round.
- **AI:** Rivals see patches as "keep speed, no drift"; a rival with low skill sometimes brakes on it
  (and goes in). Respawn as for chasms.
- **Code:** `TrackLayout.thinIce` (list of polygons + refreeze time), `TrackThinIce` (load per patch,
  crack/broken state, rendering crack overlay), `CarTerrain` (fall-through like a chasm),
  `AIDriverInput` (don't brake/drift on it).

### M2 · Collapsing cornices (`cornices`: lap from/to, side)
- **How it works:** On the outside of a ridge bend the road has a snow overhang (cornice) that is part of the road.
  When the **first car** passes, it starts to crumble: chunks break away in a wave along the bend, and the road
  there gets 2–2.5 m narrower for the rest of the race. If you're on a chunk when it breaks, you fall
  with it. The leader gets the full width; everyone behind gets a narrower bend.
- **Telegraph:** The cornice is drawn as a curled white lip with a dark crack line along its root;
  when it is about to go, snow dust puffs up along the crack and it sags (darker shade) 0.6 s before
  falling. The broken edge stays as jagged blue ice (new chasm edge).
- **AI:** Rivals hold the inside line on cornice bends; the racing line moves in once a cornice is gone.
- **Code:** `TrackLayout.cornices`, `TrackCornices` (state, chunk timing, dynamic chasm edge),
  `Track.HalfWidthAt` / off-road edge per side becomes dynamic there, renderer for lip + breaking
  chunks (falling pieces fly outward and shrink).

### M3 · Swaying rope bridges (`ropeBridges`: lap from/to, sway amplitude, period, phase)
- **How it works:** A plank deck on ropes over the void. The whole deck swings slowly sideways
  (±1.5–2.5 m, period 3–4 s), so the road shifts under you: your car is carried with the deck,
  but steering is on you to stay on it, and the deck is only 5.5 m wide with rope rails that don't stop you.
  Off the deck = fall, carried back to the bridge head.
- **Telegraph:** The deck and its ropes visibly swing; the shadow of the bridge on the glacier below
  shows the motion; creaking wood sound; the rope handrails sag towards the side it's swinging to.
  Each bridge sways a little more than the one before.
- **AI:** Rivals aim at the deck's current centre (they read the moving offset); weak rivals react late.
- **Code:** `TrackLayout.ropeBridges`, `TrackRopeBridges` (current offset per bridge), `Track`
  lateral offset of the road centre on those stretches (`CentreShiftAt(distance, time)`), deck/rope
  rendering (like `platforms`, but moving), car carried along with the deck's sideways velocity.

### M4 · Ski flying (a gap kind `skiFlying` with a landing strip)
- **How it works:** A full-width take-off at the end of a steep in-run. The flight is long (2–2.5 s) with a short
  slow motion at its top, and in the air **you steer** (far more than on a normal jump: about 4 m to each side).
  The landing is a narrow strip (the K-line zone, ~6 m wide) on a curved hill; land on it and you get a big
  boost ("TELEMARK!"); land beside it = off the cliff, carried back up to the in-run start.
- **Telegraph:** The landing strip is clearly marked: red K-line, blue distance lines and a shadow of the car
  showing where you'll land, all visible from the take-off on.
- **AI:** Rivals aim at the strip; by skill, a share of them land wide.
- **Code:** gap flag `skiFlying` + `landingWidth`, `CarMovement` air steering strength per flight
  (`Launch(..., airSteer)`), `CarTerrain` landing check against the strip, `TrackFeatures` in-run and
  hill drawing, landing-shadow marker.

### Reused as glue
`chasms` (open edges everywhere, carried back), `platforms` (ladder decks), `slopes` (fast in-run and
glacier descent), `dashPanels` (4 boost pads), `rides` style rope team as scenery only.

## 3. New props and scenery (procedural, winter palette, no glowing lamps)

- Glacier ground: blue-white ice with flow lines, moraine stripes (grey rubble), crevasses as deep
  blue slits.
- **Seracs**: huge stacked blue ice blocks with white tops (solid, cast long shadows).
- **Crevasse ladders**: aluminium ladders lashed together as the deck over two crevasses.
- **Rope team**: 4 climbers roped together, walking up beside the road (animated), ice axes.
- **Summit cross** and a stone **cairn** with prayer-flag-like pennants (coloured, not lit).
- **Rope bridges**: plank decks, rope rails, rock pillars at both ends.
- **Ski-flying hill**: judges' tower, flag line along the in-run, distance boards, stands with a crowd
  far below, a giant number board.
- **Cornices**: curled snow lips with crack lines; jagged blue break edges after the collapse.
- Distant peaks around the edge of the map (camera reveals at summit A and at the ski-flying take-off).

## 4. Build phases (each ends playable, tested on autopilot with screenshots, committed)

1. **P1 Road & world:** new waypoints, widths, slopes, chasms, glacier ground and scenery, the journey
   finish; autopilot run end to end.
2. **P2 Thin ice:** M1 + AI. ✔ fast line holds, slow/drift breaks, rival holes visible.
3. **P3 Cornices:** M2 + AI. ✔ leader gets full width, later laps narrower, fall works.
4. **P4 Rope bridges:** M3 + AI. ✔ carried with the deck, falls fair, rivals cross mostly.
5. **P5 Ski flying:** M4 + AI. ✔ steer onto strip, boost; miss = back to in-run.
6. **P6 Props & polish:** all props, camera reveals, combo words, sounds.
7. **P7 Balance:** 4 boost pads in place, rival runs → star times (6th ×0.95 / 3rd ×0.95 /
   fastest ×0.95), preview `PreviewGenerator.Generate("Winter07")`, info text.

Guard rails as always: check Play mode before every Editor step; back up and restore saves around
tests; commit after "compile: ok"; no iOS build unless asked.
