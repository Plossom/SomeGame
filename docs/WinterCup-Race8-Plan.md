# Winter Cup · Race 8 — "The Frost Citadel" — design plan

Replaces The Grand Finale. LevelDefinition stays `Winter06` (saves are keyed by it); the track asset
`NorthernLights.asset` is rebuilt from scratch (new waypoints, new everything, **no northern lights**,
no glowing lamps). Display name: **The Frost Citadel**.

Goal: the final boss of the Winter Cup. You storm a frozen fortress on top of the mountain and
race back down its Olympic bobsled run. It's harder and grander than race 7: more mechanics, longer,
narrower, with timing puzzles at speed. A clean run feels heroic; one mistake costs seconds, two cost
the podium. Falls carry you back just before the danger, as everywhere else.

One lap, about **3,100 m**, ~3:00 for a good run (a journey from the valley gate to the finish arena).

---

## 1. The line (top-down sketch)

```
                     ╔══════════ THE FROST CITADEL ═══════════╗
                     ║  (C) ICICLE GATES   ▶ throne hall ▶    ║
                     ║   portcullis rhythm      (D) TILTING   ║
                     ║        ▲                  SLABS over   ║
                     ╚════════╪═════ drawbridge ═══ the moat ══╝
                              │                       │
          (B) SERPENTINE  ◀── switchbacks up the wall  │ (E) SHUTTLE FLOES
              of the citadel rock                      │  across the frozen
                              │                       │  abyss: ride the moving
              (A) the valley road                      │  slabs from pillar to pillar
     START ═══ frozen lake gate                        ▼
                                          ╭── (F) BOBSLED RUN ──╮
     FINISH ══ arena ◀── (G) GROOMER      │  ice channel down   │
                         DESCENT ◀────────╯  the mountain: speed│
                         snowcats on the     decides how high   │
                         narrow piste        you ride the wall  │
```

| # | Section | Lap m | Width | What happens |
|---|---------|-------|-------|--------------|
| A | **Valley gate** | 0–400 | 10 → 8 | Across a frozen lake through the outer gate, ice-knight statues, the citadel towering ahead (camera reveal). Boost pad 1 at ~150. |
| B | **Citadel serpentine** | 400–900 | 7 | Tight switchbacks up the citadel rock, open drop on the outside of every hairpin (chasms). Pure driving test. |
| C | **Icicle gates** | 900–1,250 | 7 | Through the fortress: three **icicle portcullises** (new mechanic 1) in a row, each with its own rhythm, then the throne hall (cave stretch). |
| D | **Tilting slabs** | 1,250–1,600 | 6 | Over the moat on **tilting ice slabs** (new mechanic 2): five seesaw slabs on pivots; hit them off-centre and they tip you off. |
| E | **Shuttle floes** | 1,600–1,950 | 6 | Across the frozen abyss on **shuttle floes** (new mechanic 3): ice slabs gliding between stone pillars; time the jump-on, ride, and get off. Boost pad 2 at ~1,990 (solid ground after the floes, well before the bobsled start). |
| F | **Bobsled run** | 1,950–2,650 | 5.5 + walls | The Olympic **bobsled channel** (new mechanic 4): banked ice walls, a corkscrew, a "Kreisel" (270° turn). Speed decides how high you ride the wall: too slow and you scrape and stall, too fast and you fly over the lip into the abyss. Boost pad 3 at ~2,300 on the straight between corners (not before a lip). |
| G | **Groomer descent** | 2,650–3,000 | 7 | Steep narrow piste with a **snowcat convoy** (new mechanic 5) grooming its way down; pass through gaps between them, avoid their blades and tracks. |
| H | **Finish arena** | 3,000–3,100 | 12 | Into the arena: stands, flags, confetti, FINISH. Boost pad 4 at ~3,020 (after the last snowcat). |

Difficulty curve: A easy → B hard driving → C timing → D precision → E timing + precision →
F speed control at the limit → G traffic under pressure → H celebrate. Harder than race 7 in every
section from B on.

## 2. New mechanics (none of these exist yet)

### M1 · Icicle portcullises (`portcullis`: lap distance, period, open share, phase)
- **How it works:** A wall of giant icicles hangs in a gateway. In a rhythm it **drops** to the road (blocks
  the whole gate) and lifts again. Under it when it drops = smashed, shards scatter, you're spun out and
  pushed back out of the gate (lose ~2 s); not deadly, but three in a row with different rhythms punish
  bad timing.
- **Telegraph:** Its shadow on the road grows as it lifts and shrinks as it drops; icicles shiver and drip
  0.7 s before it falls; a rising chime-and-crack sound. A ring of frost on the road shows the danger zone.
- **AI:** Rivals read the rhythm: slow down/speed up to hit the open window (skill-based mistakes).
- **Code:** `TrackLayout.portcullis`, `TrackPortcullis` (state, shadow, hit test), `CarMovement.Bump`
  + spin, gate art.

### M2 · Tilting ice slabs (`tiltSlabs`: lap from/to, pivot, max tilt)
- **How it works:** Seesaw slabs of ice over the moat, each on a pivot along its centre line. Your weight
  tips it: off-centre, the slab tilts towards your side and you slide outward (sideways push grows with
  tilt); past the edge = fall into the moat, carried back to the slab start. Driving the centre line keeps
  it level. Rivals tip it too (the slab you arrive on may already lean).
- **Telegraph:** The slab is drawn with a bright centre stripe (the safe line) and shading that shows the tilt
  (the low side darkens, the high side brightens, the shadow shifts); creak sound as it tips.
- **AI:** Rivals steer for the centre stripe; weaker ones wander and slide.
- **Code:** `TrackLayout.tiltSlabs`, `TrackTiltSlabs` (tilt per slab from cars on it, damping),
  `CarMovement` sideways push by tilt, slab rendering with tilt shading.

### M3 · Shuttle floes (`shuttles`: path points, slab size, period, phase)
- **How it works:** Over a bottomless gap, flat ice slabs glide back and forth between stone pillars
  (on rails of frozen chains). The road ends at a pillar; you drive onto a passing slab and ride it to the next
  pillar. Mistime it and you drive into the void (carried back to the last pillar). Two stages with
  one and two slabs.
- **Telegraph:** Each slab's rail line is drawn across the gap, so you see where it goes; its shadow far
  below; a bell clinks when a slab docks at the pillar; the pillar edge has a red-white "wait" stripe that
  turns green-white while a slab is docked (painted colours, not lights).
- **AI:** Rivals wait at the pillar edge for the docked slab (deliberate slowdown), skill sets how well.
- **Code:** `TrackLayout.shuttles`, `TrackShuttles` (positions over time, "carried" like turntables),
  `Track` road presence over the gap = slab footprint, pillar/slab/rail art.

### M4 · Bobsled channel (`bobsled`: lap from/to, wall height per bend)
- **How it works:** A U-shaped ice channel: the road is the floor plus two curved walls. In a bend the car
  rides up the outer wall according to speed (centrifugal): too slow → it slides down and scrapes
  (speed loss); right → it carves high and fast (bonus speed on exit, "CARVE!"); too fast → it flies
  over the wall's lip and falls, carried back to the bend's start. The channel's floor is narrow
  (5.5 m); the walls are 2.5 m each.
- **Telegraph:** Each bend has a painted speed board before it (a red max, a green sweet spot) and the
  wall's height is drawn with shading and lines; the lip glints white. A wind/rumble sound rises with
  wall height.
- **AI:** Rivals brake to the board speed; weaker ones over- or undershoot.
- **Code:** `TrackLayout.bobsled`, `TrackBobsled` (wall-ride height from speed and curvature, lip
  check), `CarMovement` (wall position as sideways offset, carve boost), channel rendering.
  (Builds on the existing `banks` look, but with an overshoot risk and a full channel.)

### M5 · Snowcat convoy (`groomers`: lap from/to, count, speed, lanes)
- **How it works:** Big snowcats crawl down the narrow piste at ~40 % of race speed, side by side and
  staggered, switching lanes slowly. They are solid and heavy: hit their rear and you bounce back,
  hit their front blade and you are flung sideways. Their tiller leaves a corduroy stripe that is
  slightly slower. Pass through gaps between them; the gaps open and close as they switch lanes.
- **Telegraph:** Orange beacons painted on the roof (no glow), turn indicators showing the next lane
  change 1 s ahead, engine rumble, corduroy trails showing their lines.
- **AI:** Rivals pick gaps; they also bump into them sometimes.
- **Code:** `TrackLayout.groomers`, `TrackGroomers` (kinematic movers with colliders, lane plan),
  snowcat art (top-down, animated tracks), corduroy trail decal.

### Reused as glue
`chasms` (open edges, carried back), `caves` (throne hall), `platforms`/`viaducts` (drawbridge, rock
paths), `slopes` (fast descent), `dashPanels` (4 boost pads), `snowTunnels` (bobsled start house).

## 3. New props and scenery (procedural, winter palette, no glowing lamps, no aurora)

- **The Frost Citadel**: ice walls with battlements, round towers with pointed snow roofs, banners
  (cloth, not lit), the drawbridge, the icicle portcullises, a throne hall with a frozen throne and pillars.
- **Ice-knight statues** lining the valley road and the throne hall (giant knights holding swords).
- **Moat**: dark, deep, misty abyss under the slabs; frozen chains.
- **Stone pillars** and shuttle rails for the floes.
- **Bobsled run**: white ice channel with red/blue stripes, start house, timing boards, crowd at the
  Kreisel, sponsor-style boards (fictional).
- **Snowcats** (orange, animated tracks and tiller) and corduroy trails.
- **Finish arena**: stands, flags, confetti.

## 4. Build phases (each ends playable, tested on autopilot with screenshots, committed)

1. **P1 Road & world:** waypoints, widths, slopes, chasms, citadel walls as scenery; autopilot run.
2. **P2 Icicle gates** (M1) + AI.
3. **P3 Tilting slabs** (M2) + AI.
4. **P4 Shuttle floes** (M3) + AI.
5. **P5 Bobsled** (M4) + AI.
6. **P6 Snowcats** (M5) + AI.
7. **P7 Props & polish:** citadel, statues, arena, camera reveals, combo words, sounds.
8. **P8 Balance:** 4 boost pads, rival runs → star times (6th ×0.95 / 3rd ×0.95 / fastest ×0.95),
   preview `PreviewGenerator.Generate("Winter06")`, info text, new display name.

Guard rails as always: check Play mode before every Editor step; back up and restore saves around
tests; commit after "compile: ok"; no iOS build unless asked.
