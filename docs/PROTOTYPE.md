# SomeGame – first playable prototype

Top-down arcade racer for iPhone, portrait, one thumb, in an **Alpine Rally** look: a flat, illustrated
style in warm cream, pine green and rally orange, with hard drop shadows. The race track has
red-and-white kerbs and sandy run-off in the corners, tyre piles, lush bushes, pines, chalets, fields and
barns. The start screen is a painted forest valley with a winding road. All art is generated in the
Editor:
- `SomeGame > Generate Art`: UI shapes, icons, car, track textures and the scenery atlas.
- `SomeGame > Generate Map Art`: the map picture.
- `SomeGame > Rebuild UI (Map + Race)`: builds the Map scene and the race UI.
- `SomeGame > Create Fonts`: makes the font assets and their shadow/outline presets.

Fonts: Barlow and Barlow Condensed (SIL Open Font License, `Assets/Art/Fonts/Barlow/OFL.txt`).

## How to play

1. Pick **iPhone Portrait (1170x2532)** in the Game view and press Play: the game always starts on the
   map, whichever scene is open. To test the open scene directly (e.g. `Race.unity` on its own, which
   uses the scene's setup and gives no stars), untick `SomeGame > Play Starts On Map`.
2. **Map:** a road winds up through the forest past the five race stops (**Sunday Loop**, **Meadow Run**,
   **River Jump**, **Lagoon Leap**, **Chaos Canyon**) and disappears into the forest on the horizon. Your car waits on the road before the latest race you have reached, and the
   road behind you is orange. Tap a stop to select it. The top shows its name, laps, rivals and
   best time; the bottom shows the star times and **START**.
   Rules:
   - **All races are open from the start** (`ProgressStore.UnlockAll`; set it to false to bring back
     the rules: a race appears once the previous one is won and needs enough total stars).
   - Stars per race (0-3) depend **only on your total time** (you don't have to win, but the star
     times are fast, so it usually takes a win). Your best result per race is kept.
   - There is no garage: you drive one car, the orange buggy.
3. After **3-2-1-GO**, touch (or click) anywhere and drag. A joystick appears under your
   thumb. The car turns toward the direction the stick points (north is always up) and
   drives at full throttle once the stick leaves the small dead zone. Lift your thumb to coast.
4. **Drift and boost (Mario Kart style):** tap and hold a **second finger** anywhere on the screen.
   The car always hops. The stick at that moment decides what happens: slightly **left** or
   **right** of the car starts a drift to that side, roughly **straight** is just a hop. A drift
   can only start during the hop, so holding the finger and steering in later does nothing; lift
   and press again. A press just after turning in also counts (the car remembers your steering for
   a moment). Drifts need the road and at least 10 u/s.
   When the drift starts the car tilts into the corner but keeps going almost straight, so you
   can start it before the corner. During the drift the car always curves to its side; point the
   stick further into the corner (relative to where the car is going) to tighten it, straight or
   outward for the widest line. Sparks at the rear wheels show the charge: white (charging), then
   blue, orange and purple. Lift the second finger to fire a boost; the longer the drift, the
   stronger and longer the boost. Lifting the steering thumb, running onto the grass or getting
   rammed cancels the drift without a boost.
5. In the Editor, **WASD / arrow keys** also work: they pick one of 8 directions at full throttle.
   Hold **Space** or the **right mouse button** to drift.
6. Race against 7 rivals (8 cars); you always start from the last grid box.
   - **River Jump** crosses a river twice. Before each crossing there is a small kicker ramp with white
     arrows: hit it at speed and you fly over the water (the car grows and its shadow drops away).
     Miss it, or come in too slow, and you splash into the river and restart a little way back.
     Lakes beside the track work the same way.
   - **Oil puddles** (three to four per track): plain black; drive through one and the car spins round once
     (like a banana peel) and loses speed. Rivals steer around puddles they see coming.
   - **Corner-cut ramps** (two per track): small ramps with white arrows on the grass on the inside of
     a corner. Drive straight onto one at speed (along the arrows) and you hop across the corner and
     land on the road after it, roughly in its direction. The car keeps its own heading in the air, so
     hit the ramp at an angle and you fly at that angle. Rivals never use them.
   - Rivals are spread out: Rival 1 (front of the grid) is the fastest, each one after it a bit slower;
     Rival 7 is clearly slow and easy to pass.
   - **Sunday Loop** (race 1) is the easy warm-up: a wide oval (road 10 wide) with two long straights
     and two big curves, no oil or ramps, slower rivals.
   - Every race has **3 laps**. White **arrows** on the road shortly before every ramp show where to
     line up.
   - **Lagoon Leap** (race 3) is a causeway over water: leave the road anywhere and you splash. Tight
     zigzags, narrow passages (the road narrows and widens again), four gaps to jump (one through a
     gummiboat), floating corner cuts, jumping fish and oil.
   - **Chaos Canyon** (race 4) mixes grass, lakes and rivers and is meant to be brutal: a river jump
     on the start straight, a narrow zigzag, a lake causeway with a gap and a left-right jump-pad hop,
     a wide river crossed on two jump pads, rolling hay bales and logs crossing the road, jumping fish,
     a three-pad hop over the bottom lake, oil everywhere and a busy sky.
   - **Jump pads (gummiboats):** land on one and it throws you to the next pad (the white arrow shows
     where), then onto the road. It keeps how far off-centre you landed, so a sloppy first jump drifts
     further off each hop until you miss a pad. Steer a little in the air to correct.
   - **Jumping fish:** the water bubbles, then a big fish leaps across the road (from a pond on dry
     ground); if it hits you, you spin round once. Dodge it by timing: rush past before it jumps or
     brake and let it pass.
   - **Kickers:** hit one at a decent speed and it carries you across its gap (or onto the first pad
     in it); too slow and you fall short. Missing the kicker means the water.
   - Rivals that splash at the same spot three times are put down past the water, so they never get
     stuck for good. Pass the checkpoints in order (cutting across the grass does
   not count). Grass slows you down hard. You can push and ram the rivals.
7. The HUD at the top shows lap, race time (minutes and seconds; hundredths only on the results
   screen) and position. When you finish, the results
   screen shows your place, the stars earned and the star times, lap times and best lap,
   **MAP** (back to the map) and **RETRY**.
8. **Menu (☰, top left)** on the map and in races. In a race it **pauses** the game (timer, cars,
   countdown all freeze) with **RESUME**, **RESTART** and **MAP**. On both screens it has the
   settings: **sound volume** slider (saved, applied to all game audio) and **joystick visible/invisible**.

## Where the tunable values live

| What | Where |
|---|---|
| Player car handling: top speed, acceleration, drag (coast), turn rate, grip, high-speed grip loss, off-road slowdown/deceleration/grip, ram force, hit recovery, mass | `Assets/Data/Cars/PlayerCar.asset` (`CarStats`) |
| Drift and boost: min speed, keep-speed factor, hop window, neutral angle, steering memory, wide/tight curve rates, full-inside angle, wide/tight body angle, entry snap (kick, time, rotation speed), speed factor, charge times, boost duration/speed bonus/acceleration/kick | `PlayerCar.asset`, *Drift* and *Boost* sections |
| Drift spark colours/sizes per tier, boost flame | `Assets/Prefabs/PlayerCar.prefab`, `DriftEffects` |
| Hop height (sprite scale), duration, shadow offset | `Assets/Prefabs/Car.prefab`, `CarHop` (the sprite lives on the `Visual` child, the shadow on `Shadow`) |
| Rival handling (top speeds 18.8 down to 16.4 in steps of 0.4 for Rival 1-7, vs player 18; scaled per race: Meadow Run ×0.95, River Jump ×1.0) and cornering (`cornerGrip` 28 down to 22 on `Rival1-7` in the scene) | `Assets/Data/Cars/Rival1-7.asset` |
| Track shape (waypoints), road width, kerb width, checkpoint count, grass margin | `Assets/Data/Tracks/Circuit01.asset` (`TrackLayout`): the single source for road, off-road, checkpoints, laps and AI line |
| Texture tiling of road, grass and kerbs; which stretches count as corners (`kerbTurn`: red-and-white kerbs there, a white edge line elsewhere); sand run-off width in corners (`runoffCorner` outside, `runoffInside`); meadow patch tints; number of starting-grid boxes (`gridSlots`) | `Track` object, `TrackRenderer` component |
| Scenery: one entry per kind (tyre piles, fields, barns, chalets, bushes, trees, pines, rocks) with count, size, distance from the kerb, clustering, corner-only, angle and tints (after changes: `SomeGame > Bake Scenery (all tracks)`) | `Track` object, `TrackScenery` component (`kinds`; pictures from `Assets/Art/Rally/Scenery.png`) |
| Camera zoom (`orthographicSize`, now 16), follow smoothing, look-ahead (max 4.5, sideways × `horizontalLookAheadFactor` 0.7), framing offset, lowest car screen position (`minCarScreenHeight`, 0.44) | `Main Camera`, `FollowCamera` |
| Camera shake thresholds and strength | `Main Camera`, `CameraShake` |
| Joystick radius and dead zone | `UI/JoystickArea`, `FloatingJoystick` |
| AI lane offset, look-ahead, corner slowdown | `Rival1-7` in the scene, `AIDriverInput` |
| Lap count, countdown step | `RaceManager` (the lap count is overridden by the level when started from the map) |
| Races: name, chapter, track, laps, rival count and strength, star times, stars required | `Assets/Data/Levels/Level01-02.asset` (`LevelDefinition`), order in `LevelCatalog.asset`. Tracks: `Oval` (Sunday Loop), `Circuit01` (Meadow Run), `Circuit03` (River Jump), `Lagoon` (Lagoon Leap), `ChaosCanyon` (Chaos Canyon); `Circuit02` and the reversed `Circuit01R`, `Circuit02R` are unused for now. Levels 3 and 4 were built by an editor script from waypoints plus features placed by world position |
| Map road, race stop positions, road width | `Assets/Scripts/UI/MapLayout.cs` (then `SomeGame > Generate Map Art`, since the road is painted into the picture) |
| Map landscape (forest horizon, hills, lake, village, trees) | `Assets/Scripts/Editor/MapArtGenerator.cs` |
| Colours of the whole UI | `Assets/Scripts/UI/Theme.cs` (then rebuild the UI from the menu) |
| UI layout | `Assets/Scripts/Editor/UIBuilder.cs`. Rebuilding replaces the Map scene and the Race scene's `UI` canvas, so hand edits to the UI are lost |
| Car look | The car is an off-road buggy in two layers: `Car.png` (bodywork and roll cage, tinted with the car colour; dark cockpit, seats and wheels) and `CarDetails.png` (belts, steering wheel, lights, spare wheel; untinted). Both are generated by `ArtGenerator`. Colours: `PlayerCar.prefab` (orange) and `Rival1-7` in the scene (blue, yellow, purple, teal, pink, white, lime), sprite colour of `Visual`. `Shadow` is a soft drop shadow. Size: `Visual` scale (1.35) |
| Checkpoint posts (size, idle/next colours, pulse) | `CheckpointGates` object; number of checkpoints in `Circuit01.asset` (`checkpointCount`, 6) |
| Tyre mark width, fade, colour | `Assets/Prefabs/Car.prefab`, `TyreMarks` |
| Road width changes (`widths`: lap distance, width), water track (`waterWorld`), raised road with shadow (`elevatedRoad`), gaps (`gaps`), rivers (polyline, width, kicker position per crossing), lakes (the road crosses them on causeways), oil puddles, ramp length/width, corner cuts (`shortcuts`), jump pads (`bouncers`, with `target`), jumping fish (`geysers`: distance, side, rhythm), rolling obstacles (`sweepers`), sky life, per-track scenery | The track's `TrackLayout` (`rivers`, `lakes`, `oil`, `rampLength`, `rampWidth`). Jumps are found where a river crosses the road |
| Jump air time (`airTimeBase` + `airTimePerSpeed` × speed), air steering, landing speed and shake, oil spin (degrees, seconds, speed kept) | `PlayerCar.asset` / `Rival*.asset`, *Jumps and oil* |
| Sounds: synthesized by `SomeGame > Generate Sounds` (`Assets/Scripts/Editor/AudioGenerator.cs`) into `Assets/Audio/Sfx`; clip list and music volumes in `Assets/Resources/AudioLibrary.asset`; engine pitch range, engine/screech volumes and how far rivals are heard on the car prefab (`CarAudio`) |
| Music: map "Good Morning" by Cakeflaps, race "Pure Raceway" by MintoDog (both CC0, see `Assets/Audio/Music/CREDITS.txt`) | `Assets/Audio/Music` |
| Splash restart distance (at least 30 units of run-up before a ramp), minimum take-off speed, blink time | `Assets/Prefabs/Car.prefab`, `CarTerrain` |
| Water texture tiling, bank width, arrow size | `Track` object, `TrackFeatures` |
| Drift hop size (`peakScale`, 1.08) and jump size (`jumpScale`, 1.3) | `Assets/Prefabs/Car.prefab`, `CarHop` |
| Car collision shape (capsule 1.32 × 2.3, matches the buggy) | `Assets/Prefabs/Car.prefab`, `CapsuleCollider2D` |
| Jump pads, jumping fish (fish size, warning time), rolling obstacles | `Hazards` object, `TrackHazards` |
| Birds and balloons | `Main Camera`, `SkyLife` (how many: the track's `skyLife`) |
| Kicker minimum clearing speed, rival unstick after repeated splashes, corner-cut boost | `Assets/Prefabs/Car.prefab`, `CarTerrain` |
| Exhaust smoke: amount (`maxRate`, `fadeOutSpeed` = speed where it stops, `ratePerUnit`, `boostIntensity`), colours light/heavy, size and growth, pipe positions | `Assets/Prefabs/Car.prefab`, `EngineSmoke` |

Edits to `Circuit01` waypoints update the track in the Scene view right away. Select the
`Track` object to see the centre line, waypoints and checkpoint gizmos.

## Code layout (`Assets/Scripts`)

- `Track/`: `TrackLayout` (data), `TrackPath` (smoothed centre line + projection maths),
  `Track` (scene entry point), `TrackRenderer`, `TrackScenery`, `TrackSensor` (per-car position on track).
- `Car/`: `CarStats`, `ICarInput`, `CarMovement` (shared by player and AI), `TyreMarks`.
- `Input/`: `FloatingJoystick`, `PlayerCarInput`, `AIDriverInput`, `ControlSettings`.
- `Race/`: `RaceManager`, `RaceProgress`, `CheckpointGates`, `LevelDefinition`, `LevelCatalog`,
  `ProgressStore` (stars and unlocks), `GameSession` (scene switching), `BestLapStore`,
  `FrameRateBootstrap` (60 fps).
- `UI/`: map (`MapScreen`, `MapNode`, `MapLayout`), race UI (`RaceHud`, `CountdownView`, `FinishScreen`)
  and `GameMenu`. Also `Theme`, `SafeAreaFitter` and `TimeFormat`.
- `Camera/`: `FollowCamera`, `CameraShake`.
- `Editor/`: `ArtGenerator`, `MapArtGenerator`, `Sdf` (shared drawing helpers), `UIBuilder`.

## Running it on your iPhone (free Personal Team)

1. In Unity: **File > Build Profiles**, select **iOS** (already the active platform), click
   **Build**, choose the `Builds/iOS` folder (choose **Replace** if asked). This regenerates the
   Xcode project; `Builds/` is git-ignored.
2. Open `Builds/iOS/Unity-iPhone.xcodeproj` in Xcode.
3. **Xcode > Settings > Accounts**: click **+**, add your Apple ID. This creates the
   *Personal Team*.
4. In the project navigator click **Unity-iPhone** (blue icon), select the **Unity-iPhone**
   target, open **Signing & Capabilities**, tick **Automatically manage signing**, and set
   **Team** to *Your Name (Personal Team)*.
   If Xcode says the bundle identifier is not available, change it from
   `com.tibucher.somegame` to something unique (e.g. `com.yourname.somegame`). Also set
   it in Unity (**Project Settings > Player > iOS > Bundle Identifier**) so the next build matches.
5. Connect the iPhone with a cable, unlock it and tap **Trust This Computer**.
6. On the iPhone turn on **Settings > Privacy & Security > Developer Mode** and restart it
   when asked. The option appears after Xcode has seen the device once.
7. In Xcode's toolbar choose your iPhone as the run destination and press **Run** (⌘R).
8. The first launch is blocked as an untrusted developer. On the iPhone go to
   **Settings > General > VPN & Device Management**, tap your Apple ID under *Developer App*,
   tap **Trust**, then press Run in Xcode again.

Free-team limits: the app stops launching after 7 days (just Run from Xcode again), and you can
have at most 3 such apps installed at once.

## Unfinished or known to be rough

- **The Alpine Rally look has not been tested on a device yet** (checked in the Editor only).
- **AI is basic.** It follows the centre line plus a lane offset and can only lift off (cars
  have no brake), so it sometimes runs wide onto the grass at the hairpin. It does not try to
  overtake or avoid other cars. After the player finishes, rivals keep driving and their finish
  times are not shown.
- **Track limits:** there are no walls and no time penalties. Grass caps your speed at 40%, so
  leaving the road costs time. There are 6 checkpoints, shown as a pair of posts at the kerbs; you
  must drive between them (passing beside them on the grass does not count). The one you have to
  pass next glows yellow. Skipping one shows **MISSED CHECKPOINT / GO BACK** until you return; the
  lap does not count until you do. Driving backwards shows **WRONG WAY**. Values: `RaceProgress` on
  the car prefab (`gateMargin`, `missedCheckpointMargin`, `wrongWayDelay`).
- **Props are decoration only** (no collisions; that kept races loading for minutes on the phone). The
  rolling hay bales and logs are solid. The map edge is an invisible wall.
- **Scenery is baked:** it is placed in the Editor and stored in each track (the Race scene re-places
  the track it shows whenever its scenery settings change; `SomeGame > Bake Scenery (all tracks)` does
  all tracks). A race loads in about half a second in the Editor.
- **Ram force applies to every car-to-car hit**, AI vs AI included. With the joystick only the
  first touch is used. Touches that start on a button never create the joystick.
- **Keyboard steering is 8-directional only** (it is meant for testing).
- **Drift is player-only** (the AI never drifts). The drift feel was only tuned in the Editor, so
  expect to adjust the *Drift* / *Boost* values in `PlayerCar.asset` after trying it on the phone.
- **Best lap** is stored in PlayerPrefs under the track asset name (`BestLap.Circuit01`).
  Race progress is stored in PlayerPrefs (`Progress.v1`); there is no reset button yet
  (`ProgressStore.ResetAll()` clears it). Retry reloads the race with the same level.
- **Star times** (3 laps; Meadow Run 80 / 62 / 57 s, River Jump 85 / 66 / 60 s, Lagoon Leap 140 /
  110 / 100 s, Chaos Canyon 130 / 95 / 85 s) are rough estimates from 2-lap autopilot races and
  need a proper calibration.
- **Older star-time notes:** star times were calibrated from an autopilot run without drifting: 1 star = 10% slower,
  2 stars = 3% slower, 3 stars = 3% faster than the autopilot. The Grand Prix finales cannot be won
  by the autopilot, so they need drifting.
- **Audio:** music on the map and in races (fades over between them), engine, tyre screech, boost,
  jump, landing, splash, oil spin, crashes, countdown beeps, button clicks, lap chime, star dings and
  win/lose jingles. All follow the volume slider; pausing a race silences the cars but not the music.
  The sounds were only checked to be playing in the Editor, not listened to on a device. The map is
  the main menu.
- **Two races for now.** Progress moved to a new save slot (`Progress.v2`) when the map was rebuilt,
  so earlier stars from the 10-race city version are not carried over.
- **The countdown text outline doesn't render** (TMP outline needs its own material); the text is
  still readable.
- **Build warnings:** one from the `com.unity.pipeline` package (the Editor automation bridge,
  inactive in player builds), the rest are TextMeshPro shader/IL2CPP notes. All are harmless.
- **The build targets iPhone only** (`Target Device: iPhone Only`). Change it in Player Settings
  for iPad.
