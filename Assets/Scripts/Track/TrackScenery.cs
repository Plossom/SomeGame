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
        }

        [SerializeField] MeshFilter trees;
        [SerializeField] int seed = 12345;
        [Tooltip("Scenery also fills this far beyond the play area, so the world has no visible edge.")]
        [SerializeField, Min(0f)] float overscan = 14f;
        [SerializeField] Kind[] kinds = { new() };

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
            Assign(trees, ToMesh(layout.bakedScenery));
        }

        /// <summary>Everything that decides where the scenery goes; when it changes, the scenery is placed again.</summary>
        string Signature(TrackLayout layout)
        {
            var sig = new SignatureData
            {
                waypoints = layout.waypoints, roadWidth = layout.roadWidth, kerbWidth = layout.kerbWidth, spacing = layout.sampleSpacing,
                margin = layout.boundaryMargin, widths = layout.widths, rivers = layout.rivers, lakes = layout.lakes, gaps = layout.gaps,
                shortcuts = layout.shortcuts, tunnels = layout.tunnels, waterWorld = layout.waterWorld,
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
            public List<TrackLayout.Tunnel> tunnels;
            public bool waterWorld;
            public List<Kind> kinds;
            public int seed, span;
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
            var items = new List<(Vector2 p, Vector2 right, Rect uv, Color32 tint)>();

            // A track can bring its own scenery (e.g. boats and buoys for a water track).
            var useKinds = layout.scenery is { Count: > 0 } ? layout.scenery.ToArray() : kinds;
            foreach (var kind in useKinds)
            {
                int done = 0;
                for (int attempt = 0; attempt < kind.count * 30 && done < kind.count; attempt++)
                {
                    var p = new Vector2(Range(area.xMin, area.xMax), Range(area.yMin, area.yMax));
                    if (kind.clustered && Mathf.PerlinNoise(p.x * 0.045f + seed % 100, p.y * 0.045f) < 0.45f) continue;
                    float size = Range(kind.sizeRange.x, kind.sizeRange.y);
                    var projection = path.Project(p);
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
                    // Nothing grows on (under) a tunnel hill.
                    if (layout.tunnels.Count > 0 && track.TunnelDistance(p, out var tunnel) < layout.roadWidth * 0.5f + (tunnel?.hillMargin ?? 0f) + size * 0.5f + 3f) continue;
                    float r = size * 0.45f;
                    if (placed.Exists(q => (q.p - p).sqrMagnitude < (q.r + r) * (q.r + r))) continue;

                    placed.Add((p, r));
                    done++;
                    float angle = kind.alignToRoad
                        ? Mathf.Atan2(path.TangentAt(projection.Distance).y, path.TangentAt(projection.Distance).x)
                        : kind.squareAngle ? random.Next(4) * Mathf.PI * 0.5f + Range(-0.06f, 0.06f)
                        : Range(0f, Mathf.PI * 2f);
                    var right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (size * 0.5f);
                    Color32 tint = kind.tints is { Length: > 0 } ? kind.tints[random.Next(kind.tints.Length)] : Color.white;
                    items.Add((p, right, kind.uv, tint));
                }
            }

            // Draw top-down from the back so overlapping crowns read naturally.
            items.Sort((a, b) => b.p.y.CompareTo(a.p.y));
            return items.ConvertAll(i => new TrackLayout.SceneryItem { position = i.p, right = i.right, uv = i.uv, tint = i.tint });
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
