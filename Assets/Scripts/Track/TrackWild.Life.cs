using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The little lives along the course (<see cref="TrackLayout.Wild.life"/>), pure scenery: butterflies and
    /// dragonflies, fish leaping out of the water, ducks paddling along the bank, frogs hopping, herons that fly off
    /// when a boat comes close, leaves drifting downstream, bats at the cave mouths and drips inside; barrels, crates
    /// and bottles bobbing, rowboats, pirate flags; tropical fish, turtles and bubbles over the reef; surf bursting
    /// against the cliffs, driftwood and rocking bell buoys in the storm.
    /// </summary>
    public partial class TrackWild
    {
        const int LifeWaterOrder = -40, LifeUnderOrder = -42, SurfOrder = -43, LifeBankOrder = 9, LifeAirOrder = 42, DripOrder = 8;

        class Critter
        {
            public TrackLayout.LifeKind Kind;
            public SpriteRenderer Sr, Extra;
            public SpriteRenderer[] Puffs;
            public Vector2 Home, A, B, Dir, Pos;
            public float D, Lateral, Side, Phase, Scale, ExtraScale, Timer, Started = -100f, Radius, Speed;
            public int State;
            public Color Tint = Color.white; // (what it flies off as)
        }

        readonly List<Critter> _life = new();

        static readonly Color[] ButterflyTints = { new(1f, 0.85f, 0.25f), new(1f, 0.55f, 0.2f), new(0.55f, 0.75f, 1f), new(1f, 0.6f, 0.85f), new(1f, 1f, 1f) };
        static readonly Color[] LeafTints = { new(0.55f, 0.78f, 0.3f), new(0.92f, 0.78f, 0.25f), new(0.92f, 0.52f, 0.2f), new(0.72f, 0.56f, 0.3f) };
        static readonly Color[] FishTints = { new(1f, 0.55f, 0.15f), new(1f, 0.85f, 0.2f), new(0.35f, 0.65f, 1f), new(1f, 0.45f, 0.7f) };
        static readonly Color[] ShirtTints = { new(1f, 0.4f, 0.45f), new(0.35f, 0.6f, 1f), new(1f, 0.85f, 0.3f), new(0.4f, 0.85f, 0.6f), new(1f, 1f, 1f), new(0.95f, 0.55f, 0.85f) };
        static readonly Color HeronFlying = new(0.62f, 0.66f, 0.72f);

        void BuildLife(TrackPath path)
        {
            _life.Clear();
            if (_wild.life == null) return;
            for (int i = 0; i < _wild.life.Count; i++)
            {
                var life = _wild.life[i];
                var random = new System.Random(1000 + i * 7919 + (int)life.kind * 31);
                float R(float a, float b) => a + (float)random.NextDouble() * (b - a);
                int made = 0;
                for (int tries = 0; made < life.count && tries < life.count * 40; tries++)
                {
                    var c = new Critter { Kind = life.kind, Phase = R(0f, 100f), Side = random.Next(2) == 0 ? 1f : -1f };
                    if (life.spots is { Count: > 0 })
                    {
                        if (made >= life.spots.Count) break;
                        c.Home = life.spots[made];
                        var pr = path.Project(c.Home);
                        c.D = pr.Distance;
                    }
                    else if (!Place(path, life, c, R)) continue;
                    if (!Make(path, c, R, random)) continue;
                    _life.Add(c);
                    made++;
                }
            }
        }

        // Where along the course (a lap distance) and how far out from the channel's edge; kept off the islands for
        // what lives in the sea, out of the caves, ships and jumps for what lives in the channel.
        bool Place(TrackPath path, TrackLayout.Life life, Critter c, System.Func<float, float, float> R)
        {
            var kind = life.kind;
            if (kind is TrackLayout.LifeKind.Bats or TrackLayout.LifeKind.Drips)
            {
                if (_wild.caves.Count == 0) return false;
                var cave = _wild.caves[(int)R(0f, _wild.caves.Count - 0.001f)];
                float len = Mathf.Repeat(cave.y - cave.x, path.Length);
                if (kind == TrackLayout.LifeKind.Bats)
                {
                    c.D = R(0f, 1f) < 0.5f ? cave.x - R(1f, 4f) : cave.x + len + R(1f, 4f);
                    c.Lateral = R(-2f, 2f);
                }
                else
                {
                    c.D = cave.x + R(len * 0.15f, len * 0.85f);
                    c.Lateral = R(-1f, 1f) * Mathf.Max(0.3f, track.OffRoadAt(c.D) - 0.8f);
                }
                c.Home = path.PointAt(c.D) + path.NormalAt(c.D) * c.Lateral;
                return true;
            }
            if (kind == TrackLayout.LifeKind.Surf)
            {
                var ledges = _wild.ledges.FindAll(l => !l.reef);
                if (ledges.Count == 0) return false;
                var ledge = ledges[(int)R(0f, ledges.Count - 0.001f)];
                float len = Mathf.Repeat(ledge.to - ledge.from, path.Length);
                c.D = ledge.from + R(3f, Mathf.Max(3.5f, len - 3f));
                if (ledge.side != 0f) c.Side = Mathf.Sign(ledge.side);
                c.Lateral = c.Side * (track.OffRoadAt(c.D) + R(4.1f, 4.6f));
                c.Home = path.PointAt(c.D) + path.NormalAt(c.D) * c.Lateral;
                return !CrossesCourse(path, c.Home, 1.2f, c.D);
            }
            float span = life.along == Vector2.zero ? path.Length : Mathf.Repeat(life.along.y - life.along.x, path.Length);
            c.D = Mathf.Repeat((life.along == Vector2.zero ? 0f : life.along.x) + R(0f, span), path.Length);
            if (Blocked(path, c.D)) return false;
            float edge = track.OffRoadAt(c.D);
            float outward = R(life.spread.x, life.spread.y);
            // (over the channel: never closer than 0.3 to its middle line)
            c.Lateral = c.Side * Mathf.Max(outward < 0f ? 0.3f : 0f, edge + outward);
            c.Home = path.PointAt(c.D) + path.NormalAt(c.D) * c.Lateral;
            bool inChannel = life.spread.y <= 0f;
            if (!inChannel && CrossesCourse(path, c.Home, Mathf.Min(1f, outward * 0.5f), c.D)) return false; // (the bank's own edge is right there)
            // What lives ashore (in a lagoon race, where the land is islands): on an island, the beach folk near its shore,
            // the strollers on a quay.
            bool ashore = kind is TrackLayout.LifeKind.Gulls or TrackLayout.LifeKind.Crabs or TrackLayout.LifeKind.Sunbathers or TrackLayout.LifeKind.Walkers
                || (!_wild.enabled && kind is TrackLayout.LifeKind.Butterflies or TrackLayout.LifeKind.Frogs or TrackLayout.LifeKind.Herons);
            if (ashore)
            {
                var island = IslandAt(c.Home, -1f);
                if (island == null) return false;
                bool quay = island.kind == TrackLayout.IslandKind.Quay;
                if (kind == TrackLayout.LifeKind.Walkers && !quay) return false;
                if (quay && kind is not (TrackLayout.LifeKind.Walkers or TrackLayout.LifeKind.Gulls)) return false;
                if (kind is TrackLayout.LifeKind.Crabs or TrackLayout.LifeKind.Sunbathers
                    && (island.kind == TrackLayout.IslandKind.Rock || IslandAt(c.Home, -Mathf.Max(1.6f, island.beach * 0.55f)) != null)) return false;
            }
            bool sea = kind is TrackLayout.LifeKind.Flotsam or TrackLayout.LifeKind.Rowboats or TrackLayout.LifeKind.Driftwood or TrackLayout.LifeKind.Buoys
                or TrackLayout.LifeKind.Turtles or TrackLayout.LifeKind.TropicalFish or TrackLayout.LifeKind.Bubbles;
            if (sea && OnLand(c.Home, kind == TrackLayout.LifeKind.Rowboats ? 3f : 1.2f)) return false;
            return true;
        }

        // Somewhere another stretch of the course runs (a channel beside this one, or a crossing).
        bool CrossesCourse(TrackPath path, Vector2 p, float margin, float near)
        {
            var pr = path.Project(p);
            return Mathf.Abs(pr.Lateral) < track.OffRoadAt(pr.Distance) + margin;
        }

        // On solid ground: an island (a reef is water).
        bool OnLand(Vector2 p, float margin) => IslandAt(p, margin) != null;

        // The island a point is on (its shore pushed out by margin; a negative margin keeps to well inside it).
        TrackLayout.Island IslandAt(Vector2 p, float margin)
        {
            var layout = track.Layout;
            foreach (var island in layout.lagoonIslands)
            {
                if (island.kind == TrackLayout.IslandKind.Reef || island.points == null || island.points.Length < 3) continue;
                var pts = island.points;
                Vector2 centre = Vector2.zero;
                foreach (var q in pts) centre += q;
                centre /= pts.Length;
                bool inside = false;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                {
                    Vector2 a = pts[i] + (pts[i] - centre).normalized * margin, b = pts[j] + (pts[j] - centre).normalized * margin;
                    if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                }
                if (inside) return island;
            }
            return null;
        }

        // In a cave, a ship, on an aqueduct or over a jump: nothing lives there.
        bool Blocked(TrackPath path, float d)
        {
            bool In(Vector2 span, float pad) => path.DeltaDistance(span.x - pad, d) >= 0f && path.DeltaDistance(span.x - pad, d) <= Mathf.Repeat(span.y - span.x, path.Length) + pad * 2f;
            foreach (var s in _wild.caves) if (In(s, 6f)) return true;
            foreach (var s in _wild.galleons) if (In(s, 4f)) return true;
            foreach (var s in _wild.deckShips) if (In(s, 4f)) return true;
            var layout = track.Layout;
            foreach (var v in layout.viaducts) if (In(new Vector2(v.from, v.to), 2f)) return true;
            foreach (var g in layout.gaps) if (In(new Vector2(g.from, g.to), 4f)) return true;
            return false;
        }

        bool Make(TrackPath path, Critter c, System.Func<float, float, float> R, System.Random random)
        {
            Sprite S(string name) => Find(name);
            SpriteRenderer New(string sprite, float size, int order, Color tint)
            {
                var sp = S(sprite);
                if (sp == null) return null;
                var sr = Sprite(sprite, sp, c.Home, size, 0f, order);
                sr.color = tint;
                return sr;
            }
            Vector2 n = path.NormalAt(c.D), t = path.TangentAt(c.D);
            switch (c.Kind)
            {
                case TrackLayout.LifeKind.Butterflies:
                    c.Sr = New("Butterfly", R(0.7f, 0.95f), LifeAirOrder, ButterflyTints[random.Next(ButterflyTints.Length)]);
                    break;
                case TrackLayout.LifeKind.Dragonflies:
                    c.Sr = New("Dragonfly", R(0.9f, 1.1f), LifeAirOrder, Color.white);
                    c.Pos = c.A = c.Home;
                    break;
                case TrackLayout.LifeKind.LeapingFish:
                    c.Sr = New("Fish", R(1.2f, 1.6f), 16, Color.white);
                    c.Extra = New("SplashRing", 1f, LifeWaterOrder + 1, new Color(1f, 1f, 1f, 0f));
                    c.Timer = R(0.5f, 5f);
                    if (c.Sr != null) c.Sr.enabled = false;
                    break;
                case TrackLayout.LifeKind.Ducks:
                    c.Sr = New("Ducks", R(2.2f, 2.6f), LifeBankOrder, Color.white);
                    c.Radius = R(4f, 8f);
                    break;
                case TrackLayout.LifeKind.Frogs:
                    c.Sr = New("Frog", R(0.8f, 1f), LifeBankOrder, Color.white);
                    c.A = c.Home; c.B = c.Home + t * R(1.2f, 2f) * (random.Next(2) == 0 ? 1f : -1f);
                    c.Timer = R(1f, 5f);
                    break;
                case TrackLayout.LifeKind.Herons:
                    c.Sr = New("Heron", R(2.4f, 2.8f), LifeBankOrder, Color.white);
                    c.Tint = HeronFlying;
                    c.Extra = New("Bird", 2.6f, LifeAirOrder, new Color(c.Tint.r, c.Tint.g, c.Tint.b, 0f));
                    c.Dir = -n * c.Side;
                    break;
                case TrackLayout.LifeKind.Gulls:
                {
                    c.Sr = New("GullStanding", R(1.45f, 1.75f), LifeBankOrder, Color.white);
                    c.Extra = New("Gull", 2.4f, LifeAirOrder, new Color(1f, 1f, 1f, 0f));
                    float a = R(0f, Mathf.PI * 2f);
                    c.Dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    break;
                }
                case TrackLayout.LifeKind.Crabs:
                {
                    float a = R(0f, Mathf.PI * 2f);
                    c.A = c.Home; c.B = c.Home + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R(1f, 2.2f);
                    if (!OnLand(c.B, -0.8f)) return false;
                    c.Sr = New("Crab", R(0.75f, 0.95f), LifeBankOrder, Color.white);
                    c.Speed = R(0.25f, 0.4f);
                    break;
                }
                case TrackLayout.LifeKind.Sunbathers:
                    c.Sr = New("Sunbather", R(2.5f, 2.9f), LifeBankOrder, Color.white);
                    if (c.Sr != null) c.Sr.transform.rotation = Quaternion.Euler(0f, 0f, R(0f, 360f));
                    break;
                case TrackLayout.LifeKind.Walkers:
                {
                    // (a stroll along the quay, all of it on the paving)
                    float a = R(0f, Mathf.PI * 2f);
                    c.A = c.Home; c.B = c.Home + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R(4f, 10f);
                    for (int k = 1; k <= 4; k++)
                        if (IslandAt(Vector2.Lerp(c.A, c.B, k / 4f), -1.2f) is not { kind: TrackLayout.IslandKind.Quay }) return false;
                    c.Sr = New("Walker", R(1.75f, 2f), LifeBankOrder, ShirtTints[random.Next(ShirtTints.Length)]);
                    c.Speed = R(0.9f, 1.3f);
                    break;
                }
                case TrackLayout.LifeKind.Leaves:
                    c.Sr = New("Leaf", R(0.5f, 0.75f), LifeWaterOrder, LeafTints[random.Next(LeafTints.Length)]);
                    c.Speed = R(0.7f, 1.2f);
                    break;
                case TrackLayout.LifeKind.Bats:
                    c.Sr = New("Bat", R(0.8f, 1f), LifeAirOrder, Color.white);
                    break;
                case TrackLayout.LifeKind.Drips:
                    c.Sr = New("SplashRing", 1f, DripOrder, new Color(0.75f, 0.9f, 1f, 0f));
                    c.Timer = R(0f, 2f);
                    break;
                case TrackLayout.LifeKind.Flotsam:
                {
                    int pick = random.Next(20);
                    string sprite = pick < 10 ? "Barrel" : pick < 17 ? "Crate" : "Bottle";
                    c.Sr = New(sprite, sprite == "Barrel" ? R(1f, 1.3f) : sprite == "Crate" ? R(1.2f, 1.5f) : 0.8f, LifeWaterOrder, Color.white);
                    c.Radius = R(0f, 360f);
                    break;
                }
                case TrackLayout.LifeKind.Driftwood:
                    c.Sr = New("Driftwood", R(2.2f, 3.2f), LifeWaterOrder, Color.white);
                    c.Radius = R(0f, 360f);
                    c.Dir = t;
                    break;
                case TrackLayout.LifeKind.Rowboats:
                    c.Sr = New("Rowboat", R(3f, 3.6f), LifeBankOrder, Color.white);
                    c.Dir = t * (random.Next(2) == 0 ? 1f : -1f);
                    c.Radius = R(6f, 12f);
                    break;
                case TrackLayout.LifeKind.Flags:
                    c.Sr = New("PirateFlag", R(4.2f, 5f), 12, Color.white);
                    c.Radius = R(10f, 35f);
                    break;
                case TrackLayout.LifeKind.TropicalFish:
                {
                    var tint = FishTints[random.Next(FishTints.Length)];
                    c.Puffs = new SpriteRenderer[5];
                    for (int k = 0; k < c.Puffs.Length; k++) c.Puffs[k] = New("TropicalFish", R(0.55f, 0.75f), LifeUnderOrder, tint);
                    c.Radius = R(2f, 3.5f);
                    c.Speed = R(0.35f, 0.6f) * (random.Next(2) == 0 ? 1f : -1f);
                    break;
                }
                case TrackLayout.LifeKind.Turtles:
                    c.Sr = New("SeaTurtle", R(2f, 2.5f), LifeUnderOrder, new Color(1f, 1f, 1f, 0.95f));
                    c.Radius = R(3f, 6f);
                    c.Speed = R(0.1f, 0.16f) * (random.Next(2) == 0 ? 1f : -1f);
                    break;
                case TrackLayout.LifeKind.Bubbles:
                    c.Sr = New("Bubbles", R(0.8f, 1.1f), LifeUnderOrder + 1, new Color(1f, 1f, 1f, 0f));
                    c.Timer = R(0f, 3f);
                    break;
                case TrackLayout.LifeKind.Surf:
                    c.Sr = New("SurfSpray", R(3.6f, 4.6f), SurfOrder, new Color(1f, 1f, 1f, 0f));
                    c.Extra = New("SplashRing", 1f, SurfOrder, new Color(1f, 1f, 1f, 0f));
                    c.Dir = n * c.Side;
                    c.Timer = R(0f, 4f);
                    break;
                case TrackLayout.LifeKind.Buoys:
                    c.Sr = New("BellBuoy", R(1.4f, 1.7f), LifeBankOrder, Color.white);
                    if (_glow != null)
                    {
                        c.Extra = Sprite("BuoyLamp", _glow, c.Home, 1.8f, 0f, LifeBankOrder + 1);
                        c.Extra.color = new Color(1f, 0.9f, 0.5f, 0f);
                    }
                    break;
            }
            if (c.Sr == null && c.Puffs == null) return false;
            if (c.Sr != null) c.Scale = c.Sr.transform.localScale.x;
            if (c.Extra != null) c.ExtraScale = c.Extra.transform.localScale.x;
            if (c.Puffs != null)
            {
                foreach (var p in c.Puffs) if (p == null) return false;
                c.Scale = c.Puffs[0].transform.localScale.x;
            }
            return true;
        }

        void UpdateLife(float dt)
        {
            if (_life.Count == 0) return;
            var path = track.Path;
            float now = Time.time;
            Vector2 player = _race != null && _race.Player != null ? (Vector2)_race.Player.transform.position : new Vector2(1e6f, 1e6f);
            foreach (var c in _life)
            {
                switch (c.Kind)
                {
                    case TrackLayout.LifeKind.Butterflies:
                    {
                        Vector2 At(float tt) => c.Home + new Vector2(Mathf.Sin(tt * 0.6f + c.Phase) * 2.4f + Mathf.Sin(tt * 1.7f + c.Phase * 2f) * 0.6f,
                            Mathf.Sin(tt * 0.45f + c.Phase * 1.3f) * 1.8f + Mathf.Sin(tt * 2.1f + c.Phase) * 0.5f);
                        Vector2 p = At(now), v = At(now + 0.05f) - p;
                        c.Sr.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, Heading(v)));
                        c.Sr.transform.localScale = new Vector3(c.Scale * (0.3f + 0.7f * Mathf.Abs(Mathf.Sin(now * 16f + c.Phase))), c.Scale, 1f);
                        break;
                    }
                    case TrackLayout.LifeKind.Dragonflies:
                    {
                        c.Timer -= dt;
                        if (c.Timer <= 0f)
                        {
                            c.Timer = 0.5f + Mathf.Repeat(c.Phase * 7.13f + now * 0.37f, 1.3f);
                            float a = Mathf.Repeat(c.Phase * 3.1f + now * 2.3f, Mathf.PI * 2f);
                            c.A = c.Home + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (1.2f + Mathf.Repeat(now * 1.7f + c.Phase, 1.6f));
                        }
                        Vector2 before = c.Pos;
                        c.Pos = Vector2.Lerp(c.Pos, c.A, Mathf.Min(1f, dt * 5f));
                        Vector2 v = c.A - before;
                        float angle = v.sqrMagnitude > 0.01f ? Heading(v) : c.Sr.transform.eulerAngles.z;
                        c.Sr.transform.SetPositionAndRotation(c.Pos, Quaternion.Euler(0f, 0f, angle));
                        c.Sr.transform.localScale = new Vector3(c.Scale * (0.9f + 0.1f * Mathf.Sin(now * 45f + c.Phase)), c.Scale, 1f);
                        break;
                    }
                    case TrackLayout.LifeKind.LeapingFish:
                        UpdateLeap(c, now, dt);
                        break;
                    case TrackLayout.LifeKind.Ducks:
                    {
                        float w = 0.6f / c.Radius;
                        float s = Mathf.Sin(now * w + c.Phase) * c.Radius, ds = Mathf.Cos(now * w + c.Phase);
                        float d = c.D + s;
                        Vector2 p = path.PointAt(d) + path.NormalAt(d) * c.Lateral;
                        Vector2 dir = path.TangentAt(d) * Mathf.Sign(ds == 0f ? 1f : ds);
                        c.Sr.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
                        c.Sr.transform.localScale = Vector3.one * c.Scale * (1f + 0.03f * Mathf.Sin(now * 3f + c.Phase));
                        break;
                    }
                    case TrackLayout.LifeKind.Frogs:
                        UpdateFrog(c, now, dt);
                        break;
                    case TrackLayout.LifeKind.Herons:
                    case TrackLayout.LifeKind.Gulls:
                        UpdateHeron(c, now, dt, player);
                        break;
                    case TrackLayout.LifeKind.Crabs:
                    {
                        // Scuttling sideways, a dash and a pause, to and fro.
                        float u = Mathf.Repeat(now * c.Speed + c.Phase, 2f);
                        float k = u < 1f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u * 1.6f)) : 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 1f) * 1.6f));
                        Vector2 across = c.B - c.A;
                        float jitter = Mathf.Sin(now * 30f + c.Phase) * 4f * (k > 0.02f && k < 0.98f ? 1f : 0f);
                        c.Sr.transform.SetPositionAndRotation(Vector2.Lerp(c.A, c.B, k), Quaternion.Euler(0f, 0f, Mathf.Atan2(across.y, across.x) * Mathf.Rad2Deg + jitter));
                        break;
                    }
                    case TrackLayout.LifeKind.Sunbathers:
                        break;
                    case TrackLayout.LifeKind.Walkers:
                    {
                        float len = Vector2.Distance(c.A, c.B);
                        // (there and back, a little stop at each end to look at the boats)
                        float trip = len / c.Speed, u = Mathf.Repeat(now + c.Phase * trip, trip * 2f + 4f);
                        float k = u < trip ? u / trip : u < trip + 2f ? 1f : u < trip * 2f + 2f ? 1f - (u - trip - 2f) / trip : 0f;
                        bool walking = u < trip || (u > trip + 2f && u < trip * 2f + 2f);
                        Vector2 dir = (u < trip + 2f ? c.B - c.A : c.A - c.B);
                        c.Sr.transform.SetPositionAndRotation(Vector2.Lerp(c.A, c.B, k), Quaternion.Euler(0f, 0f, Heading(dir)));
                        float step = walking ? Mathf.Sin(now * 9f + c.Phase) : 0f;
                        c.Sr.transform.localScale = new Vector3(c.Scale * (1f + 0.04f * step), c.Scale * (1f - 0.05f * Mathf.Abs(step)), 1f);
                        break;
                    }
                    case TrackLayout.LifeKind.Leaves:
                    {
                        float d = c.D + Mathf.Repeat(now * c.Speed + c.Phase, 40f) - 20f;
                        if (Blocked(path, d)) { c.Sr.enabled = false; break; }
                        c.Sr.enabled = true;
                        float lat = c.Lateral + Mathf.Sin(now * 0.4f + c.Phase) * 0.4f;
                        lat = Mathf.Clamp(lat, -(track.OffRoadAt(d) - 0.3f), track.OffRoadAt(d) - 0.3f);
                        c.Sr.transform.SetPositionAndRotation(path.PointAt(d) + path.NormalAt(d) * lat, Quaternion.Euler(0f, 0f, c.Phase * 40f + now * 25f * (c.Side)));
                        // (fading in and out at the ends of its drift, so it never pops)
                        float u = Mathf.Repeat(now * c.Speed + c.Phase, 40f) / 40f;
                        var col = c.Sr.color; col.a = Mathf.Clamp01(Mathf.Min(u, 1f - u) * 8f); c.Sr.color = col;
                        break;
                    }
                    case TrackLayout.LifeKind.Bats:
                    {
                        Vector2 At(float tt) => c.Home + new Vector2(Mathf.Sin(tt * 1.3f + c.Phase) * 2.6f + Mathf.Sin(tt * 3.7f + c.Phase) * 0.5f,
                            Mathf.Sin(tt * 1.9f + c.Phase * 1.7f) * 2f + Mathf.Cos(tt * 4.1f + c.Phase) * 0.4f);
                        Vector2 p = At(now), v = At(now + 0.05f) - p;
                        c.Sr.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, Heading(v)));
                        c.Sr.transform.localScale = new Vector3(c.Scale * (0.35f + 0.65f * Mathf.Abs(Mathf.Sin(now * 18f + c.Phase))), c.Scale, 1f);
                        break;
                    }
                    case TrackLayout.LifeKind.Drips:
                    {
                        c.Timer -= dt;
                        if (c.Timer <= 0f)
                        {
                            c.Timer = 0.7f + Mathf.Repeat(c.Phase * 5.3f + now * 0.71f, 1.6f);
                            c.Started = now;
                            float k = Mathf.Repeat(c.Phase * 1.7f + now * 0.53f, 1f) * 2f - 1f;
                            c.Sr.transform.position = c.Home + (Vector2)(Quaternion.Euler(0f, 0f, now * 97f) * Vector2.right) * 0.8f * k;
                        }
                        float u = Mathf.Clamp01((now - c.Started) / 0.9f);
                        c.Sr.transform.localScale = Vector3.one * c.Scale * Mathf.Lerp(0.2f, 1.2f, u);
                        c.Sr.color = new Color(0.75f, 0.9f, 1f, 0.6f * (1f - u));
                        break;
                    }
                    case TrackLayout.LifeKind.Flotsam:
                    case TrackLayout.LifeKind.Driftwood:
                    {
                        Vector2 p = c.Home + new Vector2(Mathf.Sin(now * 0.3f + c.Phase), Mathf.Cos(now * 0.25f + c.Phase)) * 0.4f
                            + c.Dir * Mathf.Sin(now * 0.05f + c.Phase) * 3f;
                        c.Sr.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, c.Radius + Mathf.Sin(now * 0.8f + c.Phase) * 12f + now * 4f));
                        c.Sr.transform.localScale = Vector3.one * c.Scale * (1f + 0.04f * Mathf.Sin(now * 2f + c.Phase));
                        break;
                    }
                    case TrackLayout.LifeKind.Rowboats:
                    {
                        float w = 0.4f / c.Radius;
                        float s = Mathf.Sin(now * w + c.Phase), ds = Mathf.Cos(now * w + c.Phase);
                        Vector2 p = c.Home + c.Dir * s * c.Radius;
                        c.Sr.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, Heading(c.Dir * Mathf.Sign(ds == 0f ? 1f : ds))));
                        c.Sr.transform.localScale = new Vector3(c.Scale * (1f + 0.07f * Mathf.Sin(now * 3f + c.Phase)), c.Scale, 1f);
                        break;
                    }
                    case TrackLayout.LifeKind.Flags:
                        c.Sr.transform.SetPositionAndRotation(c.Home, Quaternion.Euler(0f, 0f, c.Radius + Mathf.Sin(now * 0.7f + c.Phase) * 6f));
                        c.Sr.transform.localScale = new Vector3(c.Scale * (0.9f + 0.1f * Mathf.Sin(now * 6f + c.Phase)), c.Scale * (1f + 0.05f * Mathf.Sin(now * 5.3f + c.Phase)), 1f);
                        break;
                    case TrackLayout.LifeKind.TropicalFish:
                    {
                        float a = now * c.Speed + c.Phase;
                        float dart = 1f + 0.5f * Mathf.Max(0f, Mathf.Sin(now * 0.9f + c.Phase * 2f));
                        Vector2 centre = c.Home + new Vector2(Mathf.Cos(a * dart), Mathf.Sin(a * dart)) * c.Radius;
                        Vector2 tangent = new Vector2(-Mathf.Sin(a * dart), Mathf.Cos(a * dart)) * Mathf.Sign(c.Speed);
                        Vector2 side = new(-tangent.y, tangent.x);
                        for (int k = 0; k < c.Puffs.Length; k++)
                        {
                            float row = k * 0.45f, col = (k % 2 == 0 ? 1f : -1f) * (k == 0 ? 0f : 0.35f + 0.1f * k);
                            Vector2 p = centre - tangent * row + side * col + new Vector2(Mathf.Sin(now * 3f + k), Mathf.Cos(now * 2.6f + k)) * 0.08f;
                            c.Puffs[k].transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, Heading(tangent) + Mathf.Sin(now * 9f + k) * 10f));
                        }
                        break;
                    }
                    case TrackLayout.LifeKind.Turtles:
                    {
                        float a = now * c.Speed + c.Phase;
                        Vector2 p = c.Home + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * c.Radius;
                        Vector2 tangent = new Vector2(-Mathf.Sin(a), Mathf.Cos(a)) * Mathf.Sign(c.Speed);
                        c.Sr.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, Heading(tangent) + Mathf.Sin(now * 1.2f + c.Phase) * 6f));
                        c.Sr.transform.localScale = new Vector3(c.Scale * (1f + 0.07f * Mathf.Sin(now * 2.4f + c.Phase)), c.Scale, 1f);
                        break;
                    }
                    case TrackLayout.LifeKind.Bubbles:
                    {
                        c.Timer -= dt;
                        if (c.Timer <= 0f)
                        {
                            c.Timer = 1.8f + Mathf.Repeat(c.Phase * 3.3f + now * 0.41f, 2.5f);
                            c.Started = now;
                            float a = Mathf.Repeat(c.Phase + now * 1.9f, Mathf.PI * 2f);
                            c.Sr.transform.position = c.Home + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Mathf.Repeat(now * 0.77f + c.Phase, 2.5f);
                        }
                        float u = Mathf.Clamp01((now - c.Started) / 1.6f);
                        c.Sr.transform.localScale = Vector3.one * c.Scale * Mathf.Lerp(0.3f, 1f, u);
                        c.Sr.color = new Color(1f, 1f, 1f, Mathf.Sin(Mathf.PI * u) * 0.9f);
                        break;
                    }
                    case TrackLayout.LifeKind.Surf:
                    {
                        // A wave breaks against the cliff: its spray thrown up the cliff's face, a ring of foam spreading
                        // on the sea.
                        c.Timer -= dt;
                        if (c.Timer <= 0f)
                        {
                            c.Timer = 1.6f + Mathf.Repeat(c.Phase * 2.9f + now * 0.63f, 2.8f);
                            c.Started = now;
                        }
                        float u = Mathf.Clamp01((now - c.Started) / 1.1f);
                        float rise = 1f - (1f - u) * (1f - u);
                        c.Sr.transform.SetPositionAndRotation(c.Home + c.Dir * 0.4f - c.Dir * rise * 0.5f, Quaternion.Euler(0f, 0f, Heading(-c.Dir)));
                        c.Sr.transform.localScale = new Vector3(c.Scale * Mathf.Lerp(0.5f, 1.2f, rise), c.Scale * Mathf.Lerp(0.3f, 1.1f, rise), 1f);
                        c.Sr.color = new Color(1f, 1f, 1f, 0.95f * Mathf.Clamp01(u * 6f) * (1f - u * u));
                        if (c.Extra != null)
                        {
                            c.Extra.transform.position = c.Home + c.Dir * 0.6f;
                            c.Extra.transform.localScale = Vector3.one * c.ExtraScale * Mathf.Lerp(0.8f, 3.2f, rise);
                            c.Extra.color = new Color(1f, 1f, 1f, 0.6f * (1f - u));
                        }
                        break;
                    }
                    case TrackLayout.LifeKind.Buoys:
                    {
                        c.Sr.transform.SetPositionAndRotation(c.Home, Quaternion.Euler(0f, 0f, Mathf.Sin(now * 1.4f + c.Phase) * 14f));
                        c.Sr.transform.localScale = Vector3.one * c.Scale * (1f + 0.05f * Mathf.Sin(now * 1.4f + c.Phase + 1f));
                        if (c.Extra != null)
                        {
                            c.Extra.transform.position = c.Home;
                            c.Extra.color = new Color(1f, 0.9f, 0.5f, 0.45f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(now * 2.2f + c.Phase)), 6f));
                        }
                        break;
                    }
                }
            }
        }

        // A fish leaps out of the water and back in, a ring of ripples where it goes in and where it comes out.
        void UpdateLeap(Critter c, float now, float dt)
        {
            const float Air = 0.9f, Ring = 0.7f;
            if (c.State == 0)
            {
                c.Timer -= dt;
                if (c.Timer > 0f) return;
                float a = Mathf.Repeat(c.Phase * 2.3f + now * 0.9f, Mathf.PI * 2f);
                c.Dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                c.A = c.Home - c.Dir * 1.4f;
                c.B = c.Home + c.Dir * 1.4f;
                c.Started = now;
                c.State = 1;
                c.Sr.enabled = true;
            }
            float u = (now - c.Started) / Air;
            if (u <= 1f)
            {
                c.Sr.transform.SetPositionAndRotation(Vector2.Lerp(c.A, c.B, u), Quaternion.Euler(0f, 0f, Heading(c.Dir) + Mathf.Lerp(-35f, 35f, u)));
                c.Sr.transform.localScale = Vector3.one * c.Scale * (0.7f + 0.5f * Mathf.Sin(Mathf.PI * u));
            }
            else c.Sr.enabled = false;
            // The ripples: at the start, then at the end.
            if (c.Extra != null)
            {
                float r0 = (now - c.Started) / Ring, r1 = (now - c.Started - Air) / Ring;
                float r = r1 >= 0f ? r1 : r0;
                c.Extra.transform.position = r1 >= 0f ? c.B : c.A;
                c.Extra.transform.localScale = Vector3.one * c.Scale * 0.4f * Mathf.Lerp(0.3f, 1.4f, Mathf.Clamp01(r));
                c.Extra.color = new Color(1f, 1f, 1f, r >= 0f && r <= 1f ? 0.8f * (1f - r) : 0f);
            }
            if (now - c.Started > Air + Ring)
            {
                c.State = 0;
                c.Timer = 2.5f + Mathf.Repeat(c.Phase * 1.37f + now * 0.29f, 4f);
            }
        }

        // A frog sits by the water, now and then hops to its other spot (and back again).
        void UpdateFrog(Critter c, float now, float dt)
        {
            const float Hop = 0.45f;
            Vector2 from = c.State % 2 == 0 ? c.A : c.B, to = c.State % 2 == 0 ? c.B : c.A;
            if (c.Started < 0f || now - c.Started > Hop)
            {
                c.Timer -= dt;
                c.Sr.transform.position = from;
                c.Sr.transform.localScale = Vector3.one * c.Scale;
                c.Sr.transform.rotation = Quaternion.Euler(0f, 0f, Heading(to - from));
                if (c.Timer <= 0f)
                {
                    c.Started = now;
                    c.Timer = 2f + Mathf.Repeat(c.Phase * 3.7f + now * 0.37f, 4f);
                }
                return;
            }
            float u = (now - c.Started) / Hop;
            c.Sr.transform.position = Vector2.Lerp(from, to, u);
            c.Sr.transform.localScale = Vector3.one * c.Scale * (1f + 0.6f * Mathf.Sin(Mathf.PI * u));
            if (u >= 0.98f) { c.State++; c.Started = -100f; }
        }

        // A heron stands at the water's edge, turning its head; when a boat comes close it takes off and flies away
        // (rising and fading), and later is back.
        void UpdateHeron(Critter c, float now, float dt, Vector2 player)
        {
            switch (c.State)
            {
                case 0:
                {
                    c.Sr.enabled = true;
                    var col = c.Sr.color; col.a = Mathf.MoveTowards(col.a, 1f, dt); c.Sr.color = col;
                    c.Sr.transform.SetPositionAndRotation(c.Home, Quaternion.Euler(0f, 0f, Heading(c.Dir) + Mathf.Sin(now * 0.5f + c.Phase) * 30f));
                    if (((Vector2)c.Home - player).sqrMagnitude < 7f * 7f && col.a > 0.9f)
                    {
                        c.State = 1;
                        c.Started = now;
                        c.Sr.enabled = false;
                        Vector2 away = -c.Dir;
                        c.B = (away + new Vector2(away.y, -away.x) * 0.6f).normalized;
                    }
                    break;
                }
                case 1:
                {
                    float u = (now - c.Started) / 3f;
                    if (c.Extra != null)
                    {
                        Vector2 p = c.Home + c.B * (u * 14f);
                        c.Extra.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, Heading(c.B)));
                        float flap = 0.75f + 0.25f * Mathf.Sin(now * 9f), grow = 1f + u * 0.4f;
                        c.Extra.transform.localScale = new Vector3(c.ExtraScale * flap * grow, c.ExtraScale * grow, 1f);
                        c.Extra.color = new Color(c.Tint.r, c.Tint.g, c.Tint.b, Mathf.Clamp01(1f - u) * Mathf.Clamp01(u * 10f));
                    }
                    if (u >= 1f) { c.State = 2; c.Started = now; if (c.Extra != null) c.Extra.color = new Color(c.Tint.r, c.Tint.g, c.Tint.b, 0f); }
                    break;
                }
                default:
                    if (now - c.Started > 14f && ((Vector2)c.Home - player).sqrMagnitude > 20f * 20f)
                    {
                        c.State = 0;
                        var col = c.Sr.color; col.a = 0f; c.Sr.color = col;
                    }
                    break;
            }
        }
    }
}
