# Water Cup · Race 2 — "Harbor Havoc" — plan

**Pitch:** rush hour in the busiest, silliest harbour in the tropics. You don't race *round* the harbour, you race
*through* it: across a ferry's car deck, down the slide of a harbour water park, through a rising canal lock, along the
wake of a gigantic cruise ship, past cranes, seals and a fireboat. Everything moves, so every lap plays a little
differently.

Race 2, difficulty 2: three laps of about 40 s, wide enough to be fair, but full of things to choose and to dodge.

---

## 1. The lap

```
                     ╔═══════════ CRUISE SHIP (steaming slowly along the outer harbour) ═══════════╗
   (5) WAKE SURF ≈≈≈ its wake: rolling swells to chain WAVE JUMPs ≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈
        │                                                                                   │
   (4) THE LOCK ── gates open/close in rhythm ── water rises: you come out on the HIGH canal  │
        │                                                                                   │
   (3) WATER PARK: ┌ conveyor up the tower → giant yellow SLIDE (fast, risky)                 │
                   └ or the long way round past the rubber-duck basin (300 ducks to scatter)  │
        │                                                                                   │
   (2) CONTAINER PORT: gantry crane swings boxes across, a stack of containers = a kicker,    │
        jump the loaded barge                                                               │
        │                                                                                   │
   (1) THE FERRY: docked on your side → drive ON over its car deck, OUT the stern = shortcut │
                  out mid-crossing → the long way round its berth                           │
        │                                                                                   │
   START ── fish market quay, fireboat arcs over the channel ── seals on the buoys ──────────┘
```

## 2. The set pieces (new)

| # | Set piece | What happens | Why it's fun |
|---|-----------|--------------|--------------|
| A | **The Ferry** | A big car ferry shuttles across the harbour mouth on a timetable. Docked on your side: its bow ramp is down, you race up onto its car deck (painted lanes, parked cars honking, a deckhand waving), and out over the stern ramp to the far side. Mid-crossing: you take the long way round. | A **moving shortcut**: timing matters, and rivals queue up behind you. FERRY HOP! |
| B | **Container Port** | A gantry crane swings a container on its hook across the channel (a sweeper in the air, with its shadow on the water). A loaded barge lies across the lane with containers stacked like steps: jump it from a container ramp. | Old sweeper and jump systems in a new look. |
| C | **Water Park** (route choice) | A conveyor belt drags your boat up a tower, then you shoot down a twisting **giant yellow water slide** with loops, into the park pool and back out to the channel. The other way round: the **rubber duck regatta**, a basin full of ~300 rubber ducks that bob and scatter (squeak!) as you plough through. | Two very different routes. The slide is faster when there's no queue. DUCK HUNT! |
| D | **The Lock** | A canal lock chamber: the gates open and close in rhythm (sliding doors). Inside, the water rises fast and lifts the boats up like an elevator (the boats grow, shadows stretch) onto the **high canal** above the harbour, which runs on a stone aqueduct. Then a big drop back down a spillway. | A real **height change** for boats, and a spillway jump (slow motion at the top). |
| E | **Cruise Ship Wake** | A gigantic cruise ship (three screens long: pool deck, sunbathers, waving passengers, a funnel puffing smoke) steams slowly along the outer harbour. Its wake is a field of rolling swells. Surf them and chain WAVE JUMPs. Its horn blast shakes the camera. | Lagoon's swells turned into a fun ride with an enormous moving landmark. |
| F | **Fireboat** | A red fireboat by the quay sprays big arcs of water across the channel (with a rainbow in them). Drive through a jet and water droplets splash over the screen, and the jet pushes you sideways. | A new screen effect, a dodge with a little reward: a short boost. |
| G | **Seaplane** | Each lap a seaplane lands in the channel ahead of the leader, skims along, and takes off again. Overtake it before it lifts off: BEAT THE PLANE! | A moving rival that isn't a racer. |

**Little life everywhere:** seals barking on the buoys as you pass, pelicans diving for fish, fishmongers tossing fish
across the market quay (the flying-fish hazard), moored yachts bobbing on their ropes, gulls stealing chips, people on
the piers ducking when you fly over them.

**Last lap:** the sun sets over the harbour (the dusk system): the harbour lights come on, the cruise ship lights up,
and fireworks go off over the water as you cross the line.

## 3. Old mechanics brought back
- **Branches:** water park slide or duck basin; ferry or the long way round.
- **Swells** (Lagoon Loop): the cruise ship's wake.
- **Currents:** the outflow of the lock spillway.
- **Trick jump:** over the barge, or off the spillway. Swipe the arrows.
- **Drawbridge rhythm:** the lock gates.
- **Ride / flume** (Winter Cup): the slide and its conveyor.
- **Dash panels, boost rings, gummiboats, flying fish, sweepers:** the crane's container is a sweeper in the air.
- **Dusk and fireworks** on the last lap.

## 4. New systems (engine)

| System | What | Builds on |
|--------|------|-----------|
| **Moving ferry** | A platform that travels on a timetable and carries cars on its deck. The way across is open only while it's docked (the route opens and closes). | turntables / platforms, branches with `openFromLap`-style gating |
| **Lock elevator** | A stretch where the boats rise: the upstairs look (bigger cars, longer shadows) driven by the lock's water level; gates as sliding doors. | `upstairs`, sliding doors |
| **Big moving ship** | A three-screen ship sprite moving along a path, its wake made from swells following it. | swells, train-line mover |
| **Rubber ducks** | Hundreds of tiny physics bodies, pooled, that a boat pushes aside. | — |
| **Screen splash** | Droplets on the screen when you drive through spray. | UI overlay |
| **Harbour world** | Quay walls (stone with tyre fenders in place of buoys) along the edges, wooden piers on stilts with shadows, colourful harbour houses with awnings, cranes, container stacks, the market hall roof that fades as you pass under it. | lagoon renderer, houses' fading roofs |

## 5. New art
Harbour scenery sheet:
- harbour houses in 4 colours, the market hall, bollards and nets, container stacks
- the gantry crane, moored yachts, fishing boats, piers, the lighthouse at the harbour mouth
- seals, pelicans, crates of fish, lobster pots, people on the quay

Big pieces:
- the ferry (with its ramps), the cruise ship, the fireboat with its arcs, the seaplane, the barge
- the water-park slide and tower, the lock and its gates, the aqueduct

Also: the track's picture and the map stop.

## 6. Phases (each ends playable, checked with screenshots, committed)
1. **Harbour world and lap:** the track, quay walls, piers, houses, and basic scenery. Drive a lap on autopilot.
2. **Ferry** (moving shortcut) and the route choice. AI takes it when it's docked.
3. **Water park:** slide ride and duck basin.
4. **Lock:** rising water, high canal, spillway trick jump.
5. **Cruise ship and its wake;** fireboat with screen splash; seaplane.
6. **Life, last-lap dusk and fireworks, combos** (FERRY HOP!, DUCK HUNT!, BEAT THE PLANE!), AI, balancing, star times, track picture, and its place on the map.

**Scope note:** the ferry, the lock and the cruise ship are the three big new systems. If it gets too much for one
race, the lock (D) moves to race 3 or 4 and race 2 keeps A, B, C, E, F, G.
