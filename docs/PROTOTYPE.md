# SomeGame – first playable prototype

Top-down arcade racer for iPhone, portrait, one thumb, in an **Alpine Rally** look: a flat, illustrated
style in warm cream, pine green and rally orange, with hard drop shadows. The race track has
red-and-white kerbs and sandy run-off in the corners, tyre piles, lush bushes, pines, chalets, fields and
barns. The start screen is a painted alpine valley with a mountain road. All art is generated in the
Editor:
- `SomeGame > Generate Art`: UI shapes, icons, car, track textures and the scenery atlas.
- `SomeGame > Generate Map Art`: the map picture.
- `SomeGame > Rebuild UI (Map + Race)`: builds the Map scene and the race UI.
- `SomeGame > Create Fonts`: makes the font assets and their shadow/outline presets.

Fonts: Barlow and Barlow Condensed (SIL Open Font License, `Assets/Art/Fonts/Barlow/OFL.txt`).

## How to play

1. Open `Assets/Scenes/Map.unity` (first scene in the build), pick **iPhone Portrait (1170x2532)** in
   the Game view and press Play. (Opening `Race.unity` directly also works for quick testing; it then
   uses the scene's own setup and gives no stars.)
2. **Map:** a mountain road winds up the valley past the race stops (for now **Meadow Run** and
   **Pine Pass**). After them comes a "?" stop (**MORE SOON**), and the road disappears into a tunnel
   in the mountain. Your car waits on the road before the latest race you have reached, and the
   road behind you is orange. Tap a stop to select it. The top shows its name, laps, rivals and
   best time; the bottom shows the star times and **START**.
   Rules:
   - A race appears (instead of "?") once you have **won the previous race**.
   - To enter it you also need enough **total stars** (shown with a lock and the stars needed);
     Pine Pass needs 1 star.
   - Stars per race (0-3) depend **only on your total time**, and only if you **win**; otherwise 0.
     Your best result per race is kept.
   - There is no garage: you drive one car, the orange rally car.
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
6. Drive 3 laps against 3 rivals. Pass the checkpoints in order (cutting across the grass does
   not count). Grass slows you down hard. You can push and ram the rivals.
7. The HUD at the top shows lap, race time and position. When you finish, the results
   screen shows your place, the stars earned and the star times, lap times and best lap,
   **NEXT** (back to the map) and **RETRY**.
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
| Rival handling (top speed 16.6 / 17.0 / 17.4 vs player 18) | `Assets/Data/Cars/Rival1-3.asset` |
| Track shape (waypoints), road width, kerb width, checkpoint count, grass margin | `Assets/Data/Tracks/Circuit01.asset` (`TrackLayout`): the single source for road, off-road, checkpoints, laps and AI line |
| Texture tiling of road, grass and kerbs; which stretches count as corners (`kerbTurn`: red-and-white kerbs there, a white edge line elsewhere); sand run-off width in corners (`runoffCorner` outside, `runoffInside`); meadow patch tints | `Track` object, `TrackRenderer` component |
| Scenery: one entry per kind (tyre piles, fields, barns, chalets, bushes, trees, pines, rocks) with count, size, distance from the kerb, clustering, corner-only, angle and tints | `Track` object, `TrackScenery` component (`kinds`; pictures from `Assets/Art/Rally/Scenery.png`) |
| Camera zoom (`orthographicSize`, now 16), follow smoothing, look-ahead (max 4.5, sideways × `horizontalLookAheadFactor` 0.7), framing offset, lowest car screen position (`minCarScreenHeight`, 0.44) | `Main Camera`, `FollowCamera` |
| Camera shake thresholds and strength | `Main Camera`, `CameraShake` |
| Joystick radius and dead zone | `UI/JoystickArea`, `FloatingJoystick` |
| AI lane offset, look-ahead, corner slowdown | `Rival1-3` in the scene, `AIDriverInput` |
| Lap count, countdown step | `RaceManager` (the lap count is overridden by the level when started from the map) |
| Races: name, chapter, track, laps, rival count and strength, star times, stars required | `Assets/Data/Levels/Level01-02.asset` (`LevelDefinition`), order in `LevelCatalog.asset`. Tracks: `Circuit01`, `Circuit02` (reversed `Circuit01R`, `Circuit02R` are unused for now) |
| Map road, race stop positions, road width | `Assets/Scripts/UI/MapLayout.cs` (then `SomeGame > Generate Map Art`, since the road is painted into the picture) |
| Map landscape (mountains, hills, lake, village, forest) | `Assets/Scripts/Editor/MapArtGenerator.cs` |
| Colours of the whole UI | `Assets/Scripts/UI/Theme.cs` (then rebuild the UI from the menu) |
| UI layout | `Assets/Scripts/Editor/UIBuilder.cs`. Rebuilding replaces the Map scene and the Race scene's `UI` canvas, so hand edits to the UI are lost |
| Car look | The rally car has two layers: `Car.png` (body, tinted with the car colour) and `CarDetails.png` (stripes, glass, light pod, roundel; untinted). Both are generated by `ArtGenerator`. Colours: `PlayerCar.prefab` (orange) and `Rival1-3` in the scene (blue, yellow, purple), sprite colour of `Visual`. `Shadow` is a soft drop shadow. Size: `Visual` scale (1.2) |
| Checkpoint posts (size, idle/next colours, pulse) | `CheckpointGates` object; number of checkpoints in `Circuit01.asset` (`checkpointCount`, 6) |
| Tyre mark width, fade, colour | `Assets/Prefabs/Car.prefab`, `TyreMarks` |

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
- **Kerbs run along the whole circuit**, not just the corners. Trees are scenery only (no
  colliders). The map edge is an invisible wall.
- **Ram force applies to every car-to-car hit**, AI vs AI included. With the joystick only the
  first touch is used. Touches that start on a button never create the joystick.
- **Keyboard steering is 8-directional only** (it is meant for testing).
- **Drift is player-only** (the AI never drifts). The drift feel was only tuned in the Editor, so
  expect to adjust the *Drift* / *Boost* values in `PlayerCar.asset` after trying it on the phone.
- **Best lap** is stored in PlayerPrefs under the track asset name (`BestLap.Circuit01`).
  Race progress is stored in PlayerPrefs (`Progress.v1`); there is no reset button yet
  (`ProgressStore.ResetAll()` clears it). Retry reloads the race with the same level.
- **Star times** were calibrated from an autopilot run without drifting: 1 star = 10% slower,
  2 stars = 3% slower, 3 stars = 3% faster than the autopilot. The Grand Prix finales cannot be won
  by the autopilot, so they need drifting.
- **No audio yet** (the volume setting is ready for it). The map is the main menu.
- **Two races for now.** Progress moved to a new save slot (`Progress.v2`) when the map was rebuilt,
  so earlier stars from the 10-race city version are not carried over.
- **The countdown text outline doesn't render** (TMP outline needs its own material); the text is
  still readable.
- **Build warnings:** one from the `com.unity.pipeline` package (the Editor automation bridge,
  inactive in player builds), the rest are TextMeshPro shader/IL2CPP notes. All are harmless.
- **The build targets iPhone only** (`Target Device: iPhone Only`). Change it in Player Settings
  for iPad.
