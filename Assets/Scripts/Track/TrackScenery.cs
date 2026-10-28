using System;
using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Scatters scenery (trees, bushes, chalets, fields, tyre piles...) around the track, keeping clear of
    /// the road and of the sandy run-off in the corners. Each kind takes its picture from a cell of one
    /// atlas texture, so everything is a single mesh. Pure scenery: no colliders. Placing is slow, so it is
    /// done in the Editor and stored in the track (<see cref="TrackLayout.bakedScenery"/>); a race only
    /// reads it back.
    /// </summary>
    public class TrackScenery : TrackDerivedBehaviour
    {
        [Serializable]
        public class Kind
        {
            public string name = "Tree";
            [Tooltip("Area of the atlas texture holding the picture (0-1 UV).")]
            public Rect uv = new(0f, 0f, 0.5f, 0.5f);
            [Min(0)] public int count = 40;
            public Vector2 sizeRange = new(2.2f, 3.6f);
            [Tooltip("Distance from the outer kerb edge: min and max (0 = no limit).")]
            public Vector2 clearance = new(3f, 0f);
            [Tooltip("Turned to follow the road (houses), instead of a random angle.")]
            public bool alignToRoad;
            [Tooltip("Clustered in forests instead of spread evenly.")]
            public bool clustered;
            [Tooltip("Only in corners, on the sand (tyre piles).")]
            public bool cornerOnly;
            [Tooltip("Turned to a multiple of 90 degrees instead of a random angle (fields, barns).")]
            public bool squareAngle;
            [Tooltip("Each item picks one of these tints at random.")]
            public Color[] tints = { Color.white };
            [Tooltip("Only beside this stretch of the lap (lap distances from, to; may wrap past the start). (0, 0) = all round.")]
            public Vector2 along;
            [Tooltip("Tree crowns reaching over the road's edge, drawn above the cars (a forest you drive through). " +
                     "Clearance may be negative: that far over the kerb.")]
            public bool canopy;
            [Tooltip("Furniture: only inside the rooms of a house (clear of its walls). Everything else stays outside the houses.")]
            public bool indoor;
        }

        [SerializeField] MeshFilter trees;
        [SerializeField] int seed = 12345;
        [Tooltip("Scenery also fills this far beyond the play area, so the world has no visible edge.")]
        [SerializeField, Min(0f)] float overscan = 14f;
        [SerializeField] Kind[] kinds = { new() };
        [Tooltip("Sorting order of the tree crowns over the road (above the cars).")]
        [SerializeField] int canopyOrder = 30;

        [Header("Run-off (match TrackRenderer)")]
        [Tooltip("Extra clearance on the outside / inside of the tightest corners, where the sand run-off is.")]
        [SerializeField, Min(0f)] float cornerRunoff = 5f;
        [SerializeField, Min(0f)] float cornerRunoffInside = 2.5f;
        [SerializeField, Min(1f)] float runoffFullTurn = 70f;
        [SerializeField, Min(1)] int runoffSpan = 9;
        [Tooltip("Road turn (degrees over the span) that counts as a corner for tyre walls.")]
        [SerializeField] float cornerTurn = 35f;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            string signature = Signature(layout);
            bool baked = layout.bakedScenery is { Count: > 0 } && layout.bakedSignature == signature;
#if !UNITY_EDITOR
            baked |= layout.bakedScenery is { Count: > 0 }; // a build always trusts the stored scenery
#endif
            if (!baked)
            {
                layout.bakedScenery = Place(path, layout);
                layout.bakedSignature = signature;
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(layout);
#endif
            }
            Assign(trees, ToMesh(layout.bakedScenery.FindAll(i => !i.canopy)));
            var crowns = layout.bakedScenery.FindAll(i => i.canopy);
            if (crowns.Count > 0)
            {
                Assign(Canopy(), ToMesh(crowns));
                SyncCanopyMaterial();
            }
            else if (_canopy != null) _canopy.sharedMesh = null;
        }

        /// <summary>Everything that decides where the scenery goes; when it changes, the scenery is placed again.</summary>
        string Signature(TrackLayout layout)
        {
            var sig = new SignatureData
            {
                waypoints = layout.waypoints, roadWidth = layout.roadWidth, kerbWidth = layout.kerbWidth, spacing = layout.sampleSpacing,
                margin = layout.boundaryMargin, widths = layout.widths, rivers = layout.rivers, lakes = layout.lakes, gaps = layout.gaps, sections = layout.sections,
                rooms = layout.rooms, houses = layout.houses, decor = layout.decor, chasms = layout.chasms, caves = layout.caves, fjord = layout.fjord, train = layout.train,
                shortcuts = layout.shortcuts, branches = layout.branches, banks = layout.banks, waterWorld = layout.waterWorld,
                kinds = new List<Kind>(layout.scenery is { Count: > 0 } ? layout.scenery.ToArray() : kinds),
                seed = seed, overscan = overscan, runoff = new Vector4(cornerRunoff, cornerRunoffInside, runoffFullTurn, cornerTurn), span = runoffSpan,
            };
            string json = JsonUtility.ToJson(sig);
            ulong hash = 14695981039346656037UL;
            foreach (char c in json) { hash ^= c; hash *= 1099511628211UL; }
            return hash.ToString("x16");
        }

        [Serializable]
        class SignatureData
        {
            public Vector2[] waypoints;
            public float roadWidth, kerbWidth, spacing, margin, overscan;
            public List<TrackLayout.WidthKey> widths;
            public List<TrackLayout.River> rivers;
            public List<TrackLayout.Lake> lakes;
            public List<TrackLayout.Gap> gaps;
            public List<TrackLayout.Shortcut> shortcuts;
            public List<TrackLayout.Branch> branches;
            public List<TrackLayout.Bank> banks;
            public bool waterWorld;
            public List<Kind> kinds;
            public int seed, span, sections;
            public List<TrackLayout.Room> rooms;
            public List<TrackLayout.House> houses;
            public List<TrackLayout.Decor> decor;
            public List<TrackLayout.Chasm> chasms;
            public List<TrackLayout.Cave> caves;
            public TrackLayout.Fjord fjord;
            public TrackLayout.TrainLine train;
            public Vector4 runoff;
        }

        Mesh ToMesh(List<TrackLayout.SceneryItem> items)
        {
            var vertices = new List<Vector3>(items.Count * 4);
            var uvs = new List<Vector2>(items.Count * 4);
            var triangles = new List<int>(items.Count * 6);
            var tints = new List<Color32>(items.Count * 4);
            foreach (var item in items)
            {
                int first = vertices.Count;
                Vector2 right = item.right;
                TrackMeshes.AddQuad(vertices, uvs, triangles, item.position, right, new Vector2(-right.y, right.x), Vector2.one);
                var uv = item.uv;
                for (int k = first; k < uvs.Count; k++)
                    uvs[k] = new Vector2(uv.x + uvs[k].x * uv.width, uv.y + uvs[k].y * uv.height);
                for (int k = 0; k < 4; k++) tints.Add(item.tint);
            }
            return TrackMeshes.Quads("Scenery", vertices, uvs, triangles, tints);
        }

        List<TrackLayout.SceneryItem> Place(TrackPath path, TrackLayout layout)
        {
            var random = new System.Random(seed);
            float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

            Rect bounds = path.Bounds();
            float margin = layout.boundaryMargin + overscan;
            var area = Rect.MinMaxRect(bounds.xMin - margin, bounds.yMin - margin, bounds.xMax + margin, bounds.yMax + margin);

            var placed = new List<(Vector2 p, float r)>();
            var items = new List<(Vector2 p, Vector2 right, Rect uv, Color32 tint, bool canopy)>();
            var crownsPlaced = new List<(Vector2 p, float r)>();

            // A track can bring its own scenery (e.g. boats and buoys for a water track).
            var useKinds = layout.scenery is { Count: > 0 } ? layout.scenery.ToArray() : kinds;
            foreach (var kind in useKinds)
            {
                int done = 0;
                for (int attempt = 0; attempt < kind.count * 30 && done < kind.count; attempt++)
                {
                    var p = new Vector2(Range(area.xMin, area.xMax), Range(area.yMin, area.yMax));
                    // Scenery kept near a stretch of road is looked for right beside it (a thin band of the map).
                    if (!kind.indoor && kind.along != Vector2.zero && kind.clearance.y > 0f)
                    {
                        float span = Mathf.Repeat(kind.along.y - kind.along.x, path.Length);
                        float d = kind.along.x + Range(0f, span);
                        float sizeGuess = (kind.sizeRange.x + kind.sizeRange.y) * 0.5f;
                        float off = track.OffRoadAt(d) + Range(kind.clearance.x, kind.clearance.y) + sizeGuess * 0.5f;
                        p = path.PointAt(d) + path.NormalAt(d) * (random.Next(2) == 0 ? off : -off);
                    }
                    // Furniture is looked for inside the rooms (they are a small part of the map).
                    if (kind.indoor && layout.rooms is { Count: > 0 })
                    {
                        var room = layout.rooms[random.Next(layout.rooms.Count)];
                        p = new Vector2(Range(room.min.x, room.max.x), Range(room.min.y, room.max.y));
                    }
                    if (kind.clustered && Mathf.PerlinNoise(p.x * 0.045f + seed % 100, p.y * 0.045f) < 0.45f) continue;
                    float size = Range(kind.sizeRange.x, kind.sizeRange.y);
                    var projection = path.Project(p);
                    if (kind.along != Vector2.zero && !Within(projection.Distance, kind.along, path.Length)) continue;
                    if (!Indoors(layout, p, size, kind.indoor)) continue;
                    float fromKerb = Mathf.Abs(projection.Lateral) - track.OffRoadAt(projection.Distance) - size * 0.5f;
                    float turn = path.TurnAtDistance(projection.Distance, runoffSpan);
                    bool outside = turn * projection.Lateral < 0f; // left turn: the right side is outside
                    if (kind.cornerOnly && Mathf.Abs(turn) < cornerTurn) continue;
                    if (!kind.cornerOnly)
                        fromKerb -= (outside ? cornerRunoff : cornerRunoffInside) * Mathf.SmoothStep(0f, 1f, Mathf.Abs(turn) / runoffFullTurn);
                    if (fromKerb < kind.clearance.x) continue;
                    if (kind.clearance.y > 0f && fromKerb > kind.clearance.y) continue;
                    if (!layout.waterWorld && track.WaterDistance(p) < size * 0.5f + 1.2f) continue;
                    if (InShortcutPath(p, size)) continue;
                    if (OnBranch(p, size)) continue;
                    if (InMountain(layout, p, size)) continue;
                    if (InPool(layout, p, size)) continue;
                    if (NearPad(layout, p, size, kind.canopy)) continue;
                    if (track.BankAt(projection.Distance, projection.Lateral - Mathf.Sign(projection.Lateral) * (size * 0.5f + 1f), out _) != null) continue; // banked walls stay clear
                    float r = size * 0.45f;
                    var layer = kind.canopy ? crownsPlaced : placed; // crowns overlap the trees below, not each other
                    if (layer.Exists(q => (q.p - p).sqrMagnitude < (q.r + r) * (q.r + r) * (kind.canopy ? 0.3f : 1f))) continue;

                    layer.Add((p, r));
                    done++;
                    float angle = kind.alignToRoad
                        ? Mathf.Atan2(path.TangentAt(projection.Distance).y, path.TangentAt(projection.Distance).x)
                        : kind.squareAngle ? random.Next(4) * Mathf.PI * 0.5f + Range(-0.06f, 0.06f)
                        : Range(0f, Mathf.PI * 2f);
                    var right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (size * 0.5f);
                    Color32 tint = kind.tints is { Length: > 0 } ? kind.tints[random.Next(kind.tints.Length)] : Color.white;
                    if (kind.canopy) tint.a = 235; // a touch see-through: cars can just be made out underneath
                    items.Add((p, right, kind.uv, tint, kind.canopy));
                }
            }

            // Hand-placed pieces (furniture): rugs under everything, the rest in among the scenery.
            var flat = new List<(Vector2 p, Vector2 right, Rect uv, Color32 tint, bool canopy)>();
            if (layout.decor != null)
                foreach (var d in layout.decor)
                {
                    float a = d.angle * Mathf.Deg2Rad;
                    var item = (d.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (d.size * 0.5f), d.uv, (Color32)Color.white, false);
                    if (d.flat) flat.Add(item); else items.Add(item);
                }
            // Draw top-down from the back so overlapping crowns read naturally.
            items.Sort((a, b) => b.p.y.CompareTo(a.p.y));
            items.InsertRange(0, flat);
            return items.ConvertAll(i => new TrackLayout.SceneryItem { position = i.p, right = i.right, uv = i.uv, tint = i.tint, canopy = i.canopy });
        }

        // Nothing stands in a pond or pool drawn flat on the ground (a frozen pond, the hot chocolate lake).
        static bool InPool(TrackLayout layout, Vector2 p, float size)
        {
            if (layout.decor == null) return false;
            foreach (var d in layout.decor)
                if (d.flat && d.cell != null && (d.cell.Contains("Pond") || d.cell.Contains("Pool")) && (d.position - p).sqrMagnitude < Sq(d.size * 0.5f + size * 0.4f))
                    return true;
            return false;
        }

        static float Sq(float v) => v * v;

        // Furniture stays in a room, clear of its walls; trees and the rest stay outside the houses.
        static bool Indoors(TrackLayout layout, Vector2 p, float size, bool indoor)
        {
            if (indoor)
            {
                if (layout.rooms == null) return false;
                float m = size * 0.5f + 1.2f;
                foreach (var room in layout.rooms)
                    if (p.x > room.min.x + m && p.x < room.max.x - m && p.y > room.min.y + m && p.y < room.max.y - m) return true;
                return false;
            }
            if (layout.houses == null) return true;
            float r = size * 0.5f + 1.5f;
            foreach (var house in layout.houses)
                if (p.x > house.min.x - r && p.x < house.max.x + r && p.y > house.min.y - r && p.y < house.max.y + r) return false;
            return true;
        }

        MeshFilter _canopy;

        // The crowns' own layer, above the cars (same sheet as the scenery below).
        MeshFilter Canopy()
        {
            if (_canopy == null)
            {
                var t = trees.transform.Find("Canopy");
                var go = t != null ? t.gameObject : new GameObject("Canopy", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(trees.transform, false);
                _canopy = go.GetComponent<MeshFilter>();
                go.GetComponent<MeshRenderer>().sortingOrder = canopyOrder;
            }
            return _canopy;
        }

        // The crowns use the scenery's sheet (the track may swap it for its own, so this follows it).
        void LateUpdate() => SyncCanopyMaterial();

        void SyncCanopyMaterial()
        {
            if (_canopy == null) return;
            var below = trees.GetComponent<MeshRenderer>();
            var mine = _canopy.GetComponent<MeshRenderer>();
            var sheet = track.Layout != null && track.Layout.sceneryMaterial != null ? track.Layout.sceneryMaterial : below != null ? below.sharedMaterial : null;
            if (sheet != null && mine.sharedMaterial != sheet) mine.sharedMaterial = sheet;
        }

        static bool Within(float d, Vector2 range, float length)
        {
            float from = Mathf.Repeat(range.x, length), to = Mathf.Repeat(range.y, length);
            return from <= to ? d >= from && d <= to : d >= from || d <= to;
        }

        // Jump pads stay clear, and no tree crown hangs over a pad's flight (a gap in the treetops that
        // gives a hidden shortcut away to those who look).
        static bool NearPad(TrackLayout layout, Vector2 p, float size, bool canopy)
        {
            foreach (var b in layout.bouncers)
            {
                if ((p - b.position).magnitude < b.radius + size * 0.5f + 1f) return true;
                if (canopy && b.HasTarget && Track.DistanceToSegment(p, b.position, b.target) < size * 0.45f + 1.5f) return true;
            }
            return false;
        }

        // The other ways round stay clear too (ice: also its frozen margin).
        TrackPath _rails;
        Vector2[] _railsFor;

        // Nothing grows in a chasm, nor on a cave's rock walls.
        bool InMountain(TrackLayout layout, Vector2 p, float size)
        {
            float r = size * 0.5f + 1f;
            if (layout.chasms is { Count: > 0 })
            {
                if (track.ChasmAt(p)) return true;
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI * 0.25f;
                    if (track.ChasmAt(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r)) return true;
                }
            }
            if (layout.fjord != null && layout.fjord.enabled)
            {
                var f = layout.fjord;
                Vector2 local = Quaternion.Euler(0f, 0f, -f.angle) * (p - f.center);
                float rx = f.radii.x + r + 2f, ry = f.radii.y + r + 2f;
                if ((local.x / rx) * (local.x / rx) + (local.y / ry) * (local.y / ry) <= 1f) return true;
            }
            // Nor on the railway.
            if (layout.train != null && layout.train.enabled && layout.train.points != null && layout.train.points.Length >= 3)
            {
                if (_rails == null || _railsFor != layout.train.points) { _rails = new TrackPath(layout.train.points, 1f); _railsFor = layout.train.points; }
                if (Mathf.Abs(_rails.Project(p).Lateral) < 2.6f + r) return true;
            }
            if (layout.caves == null) return false;
            foreach (var cave in layout.caves)
            {
                float reach = track.Layout.roadWidth * 0.5f + track.Layout.kerbWidth + 11f + r;
                if (cave.branch < 0)
                {
                    var on = track.Path.Project(p);
                    if (track.Path.DeltaDistance(cave.from - 3f, on.Distance) >= 0f && track.Path.DeltaDistance(on.Distance, cave.to + 3f) >= 0f
                        && Mathf.Abs(on.Lateral) < reach) return true;
                }
                else if (cave.branch < track.Branches.Count)
                {
                    var route = track.Branches[cave.branch];
                    route.Project(p, out float along, out float lateral);
                    float to = cave.to <= 0f ? route.Length + cave.to : cave.to;
                    if (along >= cave.from - 3f && along <= to + 3f && Mathf.Abs(lateral) < route.HalfWidth + track.Layout.kerbWidth + 11f + r) return true;
                }
            }
            return false;
        }

        bool OnBranch(Vector2 p, float size)
        {
            foreach (var branch in track.Branches)
            {
                branch.Project(p, out _, out float lateral);
                float margin = branch.Kind == TrackLayout.BranchKind.Ice ? 3.5f : track.Layout.kerbWidth + 1.5f;
                if (Mathf.Abs(lateral) < branch.HalfWidth + margin + size * 0.5f) return true;
            }
            return false;
        }

        // The corridor a shortcut jump flies through stays clear, so a short landing is never inside a tree.
        bool InShortcutPath(Vector2 p, float size)
        {
            foreach (var ramp in track.Shortcuts)
            {
                Vector2 a = ramp.Centre - ramp.Direction * (ramp.Length * 0.5f + 3.5f), b = track.Path.PointAt(ramp.To);
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                if (Vector2.Distance(p, a + ab * t) < ramp.Width * 0.5f + 2f + size * 0.5f) return true;
            }
            return false;
        }
    }
}
