using System;
using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Draws the tunnels of a track: a grassy hill over each underground stretch (it turns see-through
    /// while the player drives inside), the dim tunnel interior with lamps along the walls, stone portals
    /// at both ends, and the road that crosses over a hill as a bridge with its shadow.
    /// </summary>
    public class TrackTunnels : TrackDerivedBehaviour
    {
        [SerializeField] MeshFilter hill;
        [SerializeField] MeshFilter hillShadow;
        [Tooltip("Pines and rocks on top of the hills (scenery atlas).")]
        [SerializeField] MeshFilter hillTrees;
        [SerializeField] MeshFilter interior;
        [SerializeField] MeshFilter lamps;
        [SerializeField] MeshFilter portals;
        [SerializeField] MeshFilter bridgeShadow;
        [SerializeField] MeshFilter bridgeRoad;
        [SerializeField] MeshFilter bridgeLines;
        [SerializeField, Min(0.1f)] float hillTile = 6f;
        [SerializeField, Min(0.1f)] float roadTile = 4f;
        [SerializeField, Min(1f)] float lampSpacing = 5f;
        [Tooltip("Hill opacity while the player is in a tunnel.")]
        [SerializeField, Range(0f, 1f)] float insideAlpha = 0.22f;
        [SerializeField, Min(0.1f)] float fadeSpeed = 4f;
        [Tooltip("Darkens the screen edges while the player is in a tunnel.")]
        [SerializeField] Sprite vignetteSprite;
        [SerializeField, Range(0f, 1f)] float flicker = 0.25f;

        Mesh _hillMesh;
        readonly List<(Mesh mesh, Color32[] colors, byte[] alpha)> _faders = new();
        float _alpha = 1f;
        Mesh _lampMesh;
        Color32[] _lampColors;
        SpriteRenderer _vignette;
        SomeGame.Car.CarLevel _player;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            _hillMesh = null;
            _faders.Clear();
            var empty = new Func<string, Mesh>(n => new Mesh { name = n, hideFlags = HideFlags.DontSave });
            if (layout.tunnels.Count == 0)
            {
                foreach (var f in new[] { hill, hillShadow, hillTrees, interior, lamps, portals, bridgeShadow, bridgeRoad, bridgeLines })
                    if (f != null) Assign(f, empty(f.name));
                return;
            }

            _hillMesh = BuildHill(path, layout, Vector2.zero, false);
            Assign(hill, _hillMesh);
            if (hillShadow != null) Assign(hillShadow, BuildHill(path, layout, new Vector2(1.4f, -1.8f), true));
            if (hillTrees != null) Assign(hillTrees, BuildHillTrees(path, layout));

            // Dim interior over the road, and lamps along both walls.
            Func<float, bool> inTunnel = d => track.InTunnel(d);
            Func<float, float> edge = d => track.OffRoadAt(d) + 0.3f;
            Assign(interior, TrackMeshes.Strip(path, d => -edge(d), d => edge(d), 1f, 4f, "TunnelInterior", new Color(0.02f, 0.03f, 0.05f, 0.6f), inTunnel));
            var lv = new List<Vector3>(); var luv = new List<Vector2>(); var lt = new List<int>(); var lc = new List<Color32>();
            foreach (var t in layout.tunnels)
            {
                float span = path.DeltaDistance(t.from, t.to);
                for (float s = 2f; s < span - 1f; s += lampSpacing)
                {
                    float d = t.from + s;
                    foreach (float side in new[] { 1f, -1f })
                    {
                        Vector2 p = path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) - 0.1f);
                        TrackMeshes.AddQuad(lv, luv, lt, p, new Vector2(0.9f, 0f), new Vector2(0f, 0.9f), Vector2.one);
                        for (int k = 0; k < 4; k++) lc.Add(new Color32(255, 214, 140, 255));
                    }
                }
            }
            _lampMesh = TrackMeshes.Quads("TunnelLamps", lv, luv, lt, lc);
            _lampColors = _lampMesh.colors32;
            Assign(lamps, _lampMesh);

            // Portals: a stone arch across the road at each tunnel mouth, facing out.
            var pv = new List<Vector3>(); var puv = new List<Vector2>(); var pt = new List<int>();
            foreach (var t in layout.tunnels)
            {
                foreach (var (d, outward) in new[] { (t.from, -1f), (t.to, 1f) })
                {
                    Vector2 tangent = path.TangentAt(d) * outward, normal = path.NormalAt(d);
                    Vector2 centre = path.PointAt(d) + tangent * 0.6f;
                    float half = track.OffRoadAt(d) + 1.4f;
                    // Texture: mouth at the bottom (v = 0) faces out of the tunnel.
                    TrackMeshes.AddQuad(pv, puv, pt, centre, normal * half * (outward > 0 ? 1f : -1f), -tangent * 1.6f, Vector2.one);
                }
            }
            Assign(portals, TrackMeshes.Quads("TunnelPortals", pv, puv, pt));

            // Bridges over the hills.
            Func<float, bool> onBridge = d => track.OnBridge(d) && !track.InTunnel(d);
            Func<float, float> roadEdge = d => track.OffRoadAt(d);
            // The bridge only casts its shadow where it is actually over the hill (not on the road beyond).
            Func<float, bool> overHill = d => onBridge(d) &&
                track.TunnelDistance(path.PointAt(d), out var tun, d, 30f) < layout.roadWidth * 0.5f + layout.kerbWidth + (tun?.hillMargin ?? 0f);
            Assign(bridgeShadow, TrackMeshes.Strip(path, d => -roadEdge(d) - 0.5f, d => roadEdge(d) + 0.5f, 1f, 4f, "BridgeShadow",
                new Color(0f, 0.05f, 0.02f, 0.35f), overHill, new Vector2(0.9f, -1.2f)));
            Assign(bridgeRoad, TrackMeshes.Strip(path, d => -roadEdge(d) - 0.02f, d => roadEdge(d) + 0.02f, (layout.roadWidth * 0.5f + layout.kerbWidth) * 2f / roadTile, roadTile, "BridgeRoad", Color.white, onBridge));
            // Edge lines on the bridge: same place and colour as the road's own edge lines, so they continue
            // seamlessly where the bridge starts and ends.
            const float edgeLine = 0.28f;
            var leftRail = TrackMeshes.Strip(path, d => roadEdge(d) - edgeLine, d => roadEdge(d), 1f, 4f, "RailL", new Color(0.97f, 0.96f, 0.93f), onBridge);
            var rightRail = TrackMeshes.Strip(path, d => -roadEdge(d) + edgeLine, d => -roadEdge(d), 1f, 4f, "RailR", new Color(0.97f, 0.96f, 0.93f), onBridge);
            var lines = new Mesh { name = "BridgeLines", hideFlags = HideFlags.DontSave };
            lines.CombineMeshes(new[] { new CombineInstance { mesh = leftRail, transform = Matrix4x4.identity }, new CombineInstance { mesh = rightRail, transform = Matrix4x4.identity } });
            if (Application.isPlaying) { Destroy(leftRail); Destroy(rightRail); } else { DestroyImmediate(leftRail); DestroyImmediate(rightRail); }
            Assign(bridgeLines, lines);
        }

        // The hill: a grid over each tunnel's area; opaque within reach of the tunnel road, with a soft
        // edge and a lighter ridge along the middle so it reads as a mound.
        Mesh BuildHill(TrackPath path, TrackLayout layout, Vector2 offset, bool shadow)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>(); var col = new List<Color32>();
            foreach (var t in layout.tunnels)
            {
                Rect box = Rect.MinMaxRect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
                float span = path.DeltaDistance(t.from, t.to);
                for (float s = 0f; s <= span; s += 1f)
                {
                    Vector2 p = path.PointAt(t.from + s);
                    box = Rect.MinMaxRect(Mathf.Min(box.xMin, p.x), Mathf.Min(box.yMin, p.y), Mathf.Max(box.xMax, p.x), Mathf.Max(box.yMax, p.y));
                }
                float reach = layout.roadWidth * 0.5f + layout.kerbWidth + t.hillMargin;
                box = Rect.MinMaxRect(box.xMin - reach - 2f, box.yMin - reach - 2f, box.xMax + reach + 2f, box.yMax + reach + 2f);
                const float cell = 1f;
                int nx = Mathf.CeilToInt(box.width / cell), ny = Mathf.CeilToInt(box.height / cell);
                int start = v.Count;
                for (int j = 0; j <= ny; j++)
                    for (int i = 0; i <= nx; i++)
                    {
                        var p = new Vector2(box.xMin + i * cell, box.yMin + j * cell);
                        float dist = DistanceToTunnel(path, t, span, p);
                        float wobble = Mathf.PerlinNoise(p.x * 0.15f, p.y * 0.15f) * 2.5f;
                        float alpha = Mathf.Clamp01((reach + wobble - dist) / 0.6f);
                        float ridge = Mathf.Clamp01(1f - dist / (reach + wobble));
                        // Light on the crest, dark toward the rim: it reads as a mound.
                        float light = Mathf.Lerp(0.55f, 1.12f, Mathf.Pow(ridge, 0.55f));
                        v.Add(p + offset);
                        uv.Add(p / hillTile);
                        col.Add(shadow ? new Color(0f, 0.05f, 0.02f, alpha * 0.32f)
                                       : new Color(Mathf.Min(1f, light), Mathf.Min(1f, light), Mathf.Min(1f, light), alpha));
                    }
                for (int j = 0; j < ny; j++)
                    for (int i = 0; i < nx; i++)
                    {
                        int a = start + j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                        tri.AddRange(new[] { a, c, b, b, c, d });
                    }
            }
            var mesh = TrackMeshes.Quads(shadow ? "TunnelHillShadow" : "TunnelHill", v, uv, tri, col);
            Fader(mesh);
            return mesh;
        }

        void Fader(Mesh mesh)
        {
            var colors = mesh.colors32;
            var alpha = new byte[colors.Length];
            for (int i = 0; i < colors.Length; i++) alpha[i] = colors[i].a;
            _faders.Add((mesh, colors, alpha));
            _alpha = 1f;
        }

        // A few pines and rocks on each hill (pictures from the scenery atlas: pine cell 0, rock cell 5).
        Mesh BuildHillTrees(TrackPath path, TrackLayout layout)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>(); var col = new List<Color32>();
            var rng = new System.Random(77);
            foreach (var t in layout.tunnels)
            {
                float span = path.DeltaDistance(t.from, t.to);
                float reach = layout.roadWidth * 0.5f + layout.kerbWidth + t.hillMargin;
                var placed = new List<Vector2>();
                for (int attempt = 0; attempt < 400 && placed.Count < span / 3f; attempt++)
                {
                    float s = (float)rng.NextDouble() * span;
                    float side = rng.NextDouble() < 0.5 ? 1f : -1f;
                    float lateral = side * (1.2f + (float)rng.NextDouble() * (reach - 3.5f));
                    Vector2 p = path.PointAt(t.from + s) + path.NormalAt(t.from + s) * lateral;
                    if (DistanceToTunnel(path, t, span, p) > reach - 2.5f) continue;
                    if (placed.Exists(q => (q - p).sqrMagnitude < 9f)) continue;
                    placed.Add(p);
                    bool rock = rng.NextDouble() < 0.3;
                    float size = rock ? 1.2f + (float)rng.NextDouble() * 0.8f : 2.4f + (float)rng.NextDouble() * 1.4f;
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    Vector2 right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (size * 0.5f);
                    int first = v.Count;
                    TrackMeshes.AddQuad(v, uv, tri, p, right, new Vector2(-right.y, right.x), Vector2.one);
                    var cell = rock ? new Rect(0.25f, 1f / 3f, 0.25f, 1f / 3f) : new Rect(0f, 0f, 0.25f, 1f / 3f);
                    for (int k = first; k < uv.Count; k++) uv[k] = new Vector2(cell.x + uv[k].x * cell.width, cell.y + uv[k].y * cell.height);
                    for (int k = 0; k < 4; k++) col.Add(Color.white);
                }
            }
            var mesh = TrackMeshes.Quads("HillTrees", v, uv, tri, col);
            Fader(mesh);
            return mesh;
        }

        void Flicker()
        {
            if (_lampMesh == null || _lampColors == null) return;
            float time = Time.time;
            for (int q = 0; q < _lampColors.Length / 4; q++)
            {
                // Mostly a gentle shimmer; now and then a lamp stutters.
                float n = Mathf.PerlinNoise(q * 3.1f, time * 2.2f);
                float stutter = Mathf.PerlinNoise(q * 7.7f, time * 0.6f) > 0.78f && Mathf.Repeat(time * 17f + q, 1f) < 0.5f ? 0.35f : 1f;
                byte a = (byte)(255 * Mathf.Clamp01((1f - flicker + flicker * n) * stutter));
                for (int k = 0; k < 4; k++) _lampColors[q * 4 + k].a = a;
            }
            _lampMesh.colors32 = _lampColors;
        }

        void UpdateVignette()
        {
            var cam = Camera.main;
            if (cam == null || vignetteSprite == null) return;
            if (_vignette == null)
            {
                var go = new GameObject("TunnelVignette");
                _vignette = go.AddComponent<SpriteRenderer>();
                _vignette.sprite = vignetteSprite;
                _vignette.sortingOrder = 38; // over everything in the world, under birds and balloons
            }
            // Cover the whole view.
            float h = cam.orthographicSize * 2.4f, w = h * cam.aspect;
            Vector2 size = vignetteSprite.bounds.size;
            _vignette.transform.SetPositionAndRotation(new Vector3(cam.transform.position.x, cam.transform.position.y, 0f), Quaternion.identity);
            _vignette.transform.localScale = new Vector3(w / size.x, h / size.y, 1f);
            float a = Mathf.MoveTowards(_vignette.color.a, _player.InTunnel ? 1f : 0f, Time.deltaTime * 3f);
            _vignette.color = new Color(1f, 1f, 1f, a);
            _vignette.enabled = a > 0.01f;
        }

        static float DistanceToTunnel(TrackPath path, TrackLayout.Tunnel t, float span, Vector2 p)
        {
            // The hill ends square at the two tunnel mouths.
            Vector2 a = path.PointAt(t.from), b = path.PointAt(t.to);
            if (Vector2.Dot(p - a, path.TangentAt(t.from)) < 0f || Vector2.Dot(p - b, path.TangentAt(t.to)) > 0f) return float.MaxValue;
            float best = float.MaxValue;
            Vector2 prev = path.PointAt(t.from);
            for (float s = 1f; s <= span + 0.5f; s += 1f)
            {
                Vector2 next = path.PointAt(t.from + Mathf.Min(s, span));
                Vector2 ab = next - prev;
                float k = Mathf.Clamp01(Vector2.Dot(p - prev, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, prev + ab * k));
                prev = next;
            }
            return best;
        }

        // While the player is underground the hill turns see-through, so the car stays visible, the screen
        // edges darken and the lamps flicker.
        void Update()
        {
            if (!Application.isPlaying || _hillMesh == null) return;
            if (_player == null)
            {
                var input = FindAnyObjectByType<SomeGame.Input.PlayerCarInput>();
                if (input != null) _player = input.GetComponent<SomeGame.Car.CarLevel>();
                if (_player == null) return;
            }
            Flicker();
            UpdateVignette();
            float target = _player.InTunnel ? insideAlpha : 1f;
            if (Mathf.Approximately(_alpha, target)) return;
            _alpha = Mathf.MoveTowards(_alpha, target, fadeSpeed * Time.deltaTime);
            foreach (var (mesh, colors, alpha) in _faders)
            {
                if (mesh == null) continue;
                for (int i = 0; i < colors.Length; i++) colors[i].a = (byte)(alpha[i] * _alpha);
                mesh.colors32 = colors;
            }
        }
    }
}
