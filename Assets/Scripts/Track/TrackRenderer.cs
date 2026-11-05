using System;
using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Draws grass, road, white edge lines, red-and-white kerbs and sandy run-off in the corners, and the
    /// start line from the track data, and fences the play area.
    /// </summary>
    public class TrackRenderer : TrackDerivedBehaviour
    {
        [SerializeField] MeshFilter grass;
        [SerializeField] MeshFilter road;
        [SerializeField] MeshFilter kerbs;
        [SerializeField] MeshFilter startLine;
        [Tooltip("White line along both road edges.")]
        [SerializeField] MeshFilter edgeLines;
        [SerializeField, Min(0.01f)] float edgeLineWidth = 0.28f;
        [Tooltip("White starting-grid boxes behind the start line.")]
        [SerializeField] MeshFilter gridLines;
        [SerializeField, Min(0)] int gridSlots = 8;
        [Tooltip("Optional sandy run-off beyond the kerbs in the corners (wider on the outside).")]
        [SerializeField] MeshFilter runoff;
        [Tooltip("Optional dashed centre line.")]
        [SerializeField] MeshFilter centerLine;
        [Tooltip("Invisible wall around the grass so cars cannot leave the map. Set up in Play mode only.")]
        [SerializeField] EdgeCollider2D boundary;

        [Header("Texture tiling (world units per texture repeat)")]
        [SerializeField, Min(0.1f)] float roadTile = 4f;
        [SerializeField, Min(0.1f)] float grassTile = 4f;
        [Tooltip("Approximate length of one edge texture repeat; adjusted so the loop closes cleanly.")]
        [SerializeField, Min(0.1f)] float kerbCycle = 2.4f;
        [Tooltip("Vertex colour of the edge strip on the left / right of the driving direction.")]
        [SerializeField] Color leftEdgeColor = Color.white;
        [SerializeField] Color rightEdgeColor = Color.white;
        [Tooltip("Length of one centre-line dash plus gap; adjusted so the loop closes cleanly.")]
        [SerializeField, Min(0.1f)] float centerLineCycle = 3f;
        [SerializeField, Min(0.01f)] float centerLineWidth = 0.22f;
        [Tooltip("Grass is drawn this far beyond the invisible wall, so the world has no visible edge.")]
        [SerializeField, Min(0f)] float grassOverscan = 30f;
        [Header("Corners")]
        [Tooltip("Road turn (degrees over the sampled span) from which a stretch counts as a corner and gets kerbs.")]
        [SerializeField, Min(1f)] float kerbTurn = 22f;
        [Tooltip("Run-off width on the outside / inside of the tightest corners (none on straights).")]
        [SerializeField, Min(0f)] float runoffCorner = 5f;
        [SerializeField, Min(0f)] float runoffInside = 2.5f;
        [Tooltip("Road turn (degrees over the sampled span) that gets the full corner width.")]
        [SerializeField, Min(1f)] float runoffFullTurn = 70f;
        [SerializeField, Min(1)] int runoffSpan = 9;
        [SerializeField, Min(0.1f)] float runoffTile = 4f;

        [Header("Water tracks")]
        [Tooltip("Ground materials: grass normally, water on a water-world track.")]
        [SerializeField] Material grassMaterial;
        [SerializeField] Material waterMaterial;
        [SerializeField, Min(0.1f)] float waterTile = 8f;
        [Tooltip("Camera background beyond the ground, on grass and water tracks.")]
        [SerializeField] Color grassBackground = new(0.373f, 0.627f, 0.247f);
        [SerializeField] Color waterBackground = new(0.25f, 0.58f, 0.74f);
        [Tooltip("Shadow of the road on the water (water tracks only).")]
        [SerializeField] MeshFilter roadShadow;
        [SerializeField] Vector2 roadShadowOffset = new(0.35f, -0.5f);

        [Header("Snow biome (Winter Cup)")]
        [SerializeField] Material snowGround;
        [SerializeField] Material snowRoad;
        [SerializeField] Material snowKerbs;
        [SerializeField] Material snowRunoff;
        [SerializeField] Material snowScenery;
        [Tooltip("The scenery mesh, which takes the winter atlas on a snow track.")]
        [SerializeField] MeshRenderer scenery;
        [SerializeField] Color snowBackground = new(0.9f, 0.94f, 0.98f);
        [Tooltip("Snowfall over the race, shown on snow tracks.")]
        [SerializeField] GameObject snowfall;

        [Header("Vehicles")]
        [Tooltip("Body (tinted), details, shadow and ski (in place of the front wheels) of the snowmobile, for tracks raced on snowmobiles.")]
        [SerializeField] Sprite snowmobileBody;
        [SerializeField] Sprite snowmobileDetails;
        [SerializeField] Sprite snowmobileShadow;
        [SerializeField] Sprite snowmobileSki;

        [Tooltip("Grass tint range for the large meadow patches.")]
        [SerializeField] Color meadowDark = new(0.8f, 0.88f, 0.8f);
        [SerializeField] Color meadowLight = Color.white;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            float hw = layout.HalfWidth, outer = hw + layout.kerbWidth;

            bool water = layout.waterWorld;
            // (the helix's road is drawn apart: see ShowHelix)
            Func<float, bool> keep = d => !InHelix(d) && !track.InGap(d);
            Func<float, float> edge = d => track.OffRoadAt(d);
            Assign(road, TrackMeshes.Strip(path, d => -edge(d) - 0.02f, d => edge(d) + 0.02f, outer * 2f / roadTile, roadTile, "Road", Color.white, keep));
            // On a water track the road stands above the water and casts a soft shadow onto it.
            if (roadShadow != null)
                Assign(roadShadow, water || layout.elevatedRoad
                    ? TrackMeshes.Strip(path, d => -edge(d) - 0.1f, d => edge(d) + 0.1f, 1f, 4f, "RoadShadow", new Color(0f, 0.05f, 0.1f, 0.3f), keep, roadShadowOffset)
                    : new Mesh { name = "RoadShadow", hideFlags = HideFlags.DontSave });
            Assign(kerbs, BuildKerbs(path, keep));
            if (edgeLines != null) Assign(edgeLines, BuildEdgeLines(path, keep));
            Assign(startLine, BuildStartLine(path, hw));
            if (gridLines != null) Assign(gridLines, BuildGrid(path, layout));
            if (runoff != null) Assign(runoff, water ? new Mesh { name = "Runoff", hideFlags = HideFlags.DontSave } : BuildRunoff(path));
            if (centerLine != null)
            {
                float cycle = path.Length / Mathf.Max(1, Mathf.Round(path.Length / centerLineCycle));
                Assign(centerLine, TrackMeshes.Strip(path, d => -centerLineWidth * 0.5f, d => centerLineWidth * 0.5f, 1f, cycle, "CenterLine", Color.white, keep));
            }

            bool snow = layout.biome == TrackLayout.Biome.Snow && !water;
            Rect area = Expand(path.Bounds(), layout.boundaryMargin);
            Assign(grass, BuildGrass(Expand(area, grassOverscan), water, snow));
            if (Application.isPlaying && Camera.main != null) Camera.main.backgroundColor = water ? waterBackground : snow ? snowBackground : grassBackground;
            var groundRenderer = grass != null ? grass.GetComponent<MeshRenderer>() : null;
            if (groundRenderer != null && grassMaterial != null && waterMaterial != null)
                groundRenderer.sharedMaterial = water ? waterMaterial : snow && snowGround != null ? snowGround : grassMaterial;
            ApplyBiome(snow);
            BuildHelix();
            if (boundary != null && Application.isPlaying)
            {
                Rect wall = Expand(area, -1f);
                boundary.points = new[]
                {
                    new Vector2(wall.xMin, wall.yMin), new Vector2(wall.xMax, wall.yMin),
                    new Vector2(wall.xMax, wall.yMax), new Vector2(wall.xMin, wall.yMax),
                    new Vector2(wall.xMin, wall.yMin),
                };
            }
        }

        // The snow biome swaps the road, kerb, run-off and scenery materials (the meadow ones are remembered
        // so a meadow track gets them back) and switches the snowfall on.
        readonly System.Collections.Generic.Dictionary<Renderer, Material> _meadow = new();

        void ApplyBiome(bool snow)
        {
            // A track with its own road surface uses it whatever the biome.
            var ownRoad = track.Layout != null ? track.Layout.roadMaterial : null;
            Swap(road, ownRoad != null ? ownRoad : snowRoad, snow || ownRoad != null);
            Swap(kerbs, snowKerbs, snow);
            Swap(runoff, snowRunoff, snow);
            Swap(scenery, snowScenery, snow);
            // A track with its own scenery sheet (the journey race) uses it instead.
            var own = track.Layout != null ? track.Layout.sceneryMaterial : null;
            if (scenery != null && own != null) scenery.GetComponent<Renderer>().sharedMaterial = own;
            if (snowfall != null && Application.isPlaying) snowfall.SetActive(snow && (track.Layout == null || !track.Layout.noSnowfall));
            if (Application.isPlaying) ApplyVehicle(track.Layout != null ? track.Layout.vehicle : TrackLayout.Vehicle.Car);
            // Powder spray, pale ruts, glitter and gusts on the snow (not on a winter night, which has its own).
            if (Application.isPlaying)
            {
                bool fx = snow && track.Layout != null && !track.Layout.dusk;
                var winter = GetComponent<WinterFx>();
                if (fx && winter == null) gameObject.AddComponent<WinterFx>().Init(null, track.Layout.vehicle == TrackLayout.Vehicle.Snowmobile);
                else if (!fx && winter != null) Destroy(winter);
            }
        }

        // Every car (player, rivals, online puppets) takes this track's vehicle: its body, details, shadow
        // and front wheel sprites swapped in (the car's own are remembered for the next track).
        readonly System.Collections.Generic.Dictionary<SpriteRenderer, Sprite> _carSprites = new();

        void ApplyVehicle(TrackLayout.Vehicle vehicle)
        {
            bool sled = vehicle == TrackLayout.Vehicle.Snowmobile && snowmobileBody != null;
            foreach (var car in FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (var sr in car.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    var sprite = sr.name switch { "Visual" => snowmobileBody, "Details" => snowmobileDetails, "Shadow" => snowmobileShadow, "WheelL" or "WheelR" => snowmobileSki, _ => null };
                    if (sprite == null) continue;
                    if (!_carSprites.ContainsKey(sr)) _carSprites[sr] = sr.sprite;
                    sr.sprite = sled ? sprite : _carSprites[sr];
                }
            }
        }

        void Swap(Component target, Material snowMaterial, bool snow)
        {
            if (target == null || snowMaterial == null) return;
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;
            if (!_meadow.ContainsKey(renderer)) _meadow[renderer] = renderer.sharedMaterial == snowMaterial ? null : renderer.sharedMaterial;
            var meadow = _meadow[renderer];
            renderer.sharedMaterial = snow ? snowMaterial : meadow != null ? meadow : renderer.sharedMaterial;
        }

        // Red-and-white kerbs on both sides, only where the road turns enough.
        Mesh BuildKerbs(TrackPath path, Func<float, bool> keep, bool helix = false)
        {
            int n = path.Count;
            float cycle = path.Length / Mathf.Max(1, Mathf.Round(path.Length / kerbCycle));
            Func<float, float> v = helix ? HelixV(cycle) : d => d / cycle;
            var corner = new bool[n];
            for (int i = 0; i < n; i++) corner[i] = keep(path.DistanceAt(i) + 0.5f) && Mathf.Abs(path.TurnAt(i, runoffSpan)) >= kerbTurn;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? 1f : -1f;
                for (int i = 0; i < n; i++)
                {
                    if (!corner[i]) continue;
                    int a = vertices.Count;
                    for (int k = 0; k <= 1; k++)
                    {
                        Vector2 p = path[i + k], normal = path.SampleNormal(i + k) * sign;
                        float d = path.DistanceAt(i);
                        if (k == 1) d += path.SegmentLength(i);
                        vertices.Add(p + normal * track.HalfWidthAt(d));
                        vertices.Add(p + normal * track.OffRoadAt(d));
                        uvs.Add(new Vector2(0f, v(d)));
                        uvs.Add(new Vector2(1f, v(d)));
                    }
                    if (side == 0) triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                    else triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
            return TrackMeshes.Quads("Kerbs", vertices, uvs, triangles);
        }

        Mesh BuildEdgeLines(TrackPath path, Func<float, bool> keep, bool helix = false)
        {
            Func<float, float> outer = d => track.OffRoadAt(d), inner = d => track.OffRoadAt(d) - edgeLineWidth;
            var v = helix ? HelixV(4f) : null;
            var left = TrackMeshes.Strip(path, inner, outer, 1f, 4f, "EdgeLeft", Color.white, keep, default, v);
            var right = TrackMeshes.Strip(path, d => -inner(d), d => -outer(d), 1f, 4f, "EdgeRight", Color.white, keep, default, v);
            var combined = new Mesh { name = "EdgeLines", hideFlags = HideFlags.DontSave };
            combined.CombineMeshes(new[]
            {
                new CombineInstance { mesh = left, transform = Matrix4x4.identity },
                new CombineInstance { mesh = right, transform = Matrix4x4.identity },
            });
            DestroyTemp(left);
            DestroyTemp(right);
            return combined;
        }

        // ------------------------------------------------------------------ the helix

        // The helix's loops lie exactly on top of each other, so its road is drawn apart from the rest of the track: only
        // one loop's worth of it at a time, round the player (TrackHelix moves it along as they climb), never the loops
        // above or below. The tunnel in and the tunnel out are drawn only while they are on the player's level.
        float _helixFrom = float.NaN, _helixTo;
        bool _helixEntry = true, _helixExit = true;
        readonly List<(MeshFilter source, MeshFilter copy)> _helixLayers = new();

        bool InHelix(float d)
        {
            var h = track.Layout != null ? track.Layout.helix : null;
            return h != null && h.enabled && d >= h.entry && d <= h.exit;
        }

        /// <summary>Draws the helix's road from <paramref name="from"/> to <paramref name="to"/> (lap distances on its loops), and its tunnels in and out if asked.</summary>
        public void ShowHelix(float from, float to, bool entry, bool exit)
        {
            if (from == _helixFrom && to == _helixTo && entry == _helixEntry && exit == _helixExit) return;
            _helixFrom = from;
            _helixTo = to;
            _helixEntry = entry;
            _helixExit = exit;
            BuildHelix();
        }

        void BuildHelix()
        {
            var h = track.Layout != null ? track.Layout.helix : null;
            if (h == null || !h.enabled)
            {
                foreach (var (_, copy) in _helixLayers) if (copy != null) copy.gameObject.SetActive(false);
                return;
            }
            var path = track.Path;
            if (float.IsNaN(_helixFrom))
            {
                _helixFrom = h.from;
                _helixTo = h.from + (h.to - h.from) / Mathf.Max(1, h.turns);
            }
            // (a little past the window's end, so the loop closes without a crack)
            Func<float, bool> shown = d => d >= h.entry && d <= h.exit && !track.InGap(d)
                && (d >= _helixFrom && d < _helixTo + 0.6f || _helixEntry && d < h.from || _helixExit && d >= h.to);
            Func<float, float> edge = d => track.OffRoadAt(d);
            float outer = track.Layout.HalfWidth + track.Layout.kerbWidth;
            Layer(road, TrackMeshes.Strip(path, d => -edge(d) - 0.02f, d => edge(d) + 0.02f, outer * 2f / roadTile, roadTile, "RoadHelix", Color.white, shown, default, HelixV(roadTile)));
            Layer(kerbs, BuildKerbs(path, shown, true));
            if (edgeLines != null) Layer(edgeLines, BuildEdgeLines(path, shown, true));
            if (centerLine != null)
            {
                float cycle = path.Length / Mathf.Max(1, Mathf.Round(path.Length / centerLineCycle));
                Layer(centerLine, TrackMeshes.Strip(path, d => -centerLineWidth * 0.5f, d => centerLineWidth * 0.5f, 1f, cycle, "CenterLineHelix", Color.white, shown, default, HelixV(cycle)));
            }
        }

        // Texture runs along the helix: each loop a whole number of repeats (so the road and kerbs meet themselves
        // seamlessly where the window closes), meeting the rest of the track's at the tunnel in and the tunnel out.
        Func<float, float> HelixV(float tile)
        {
            var h = track.Layout.helix;
            float loop = (h.to - h.from) / Mathf.Max(1, h.turns);
            float ringTile = loop / Mathf.Max(1f, Mathf.Round(loop / tile));
            float vTo = h.from / tile + (h.to - h.from) / ringTile;
            float tail = (h.exit / tile + Mathf.Round(vTo - h.to / tile) - vTo) / Mathf.Max(0.01f, h.exit - h.to);
            return d => d < h.from ? d / tile : d <= h.to ? h.from / tile + (d - h.from) / ringTile : vTo + (d - h.to) * tail;
        }

        // The helix's own copy of a road layer: same material and draw order, its own mesh.
        void Layer(MeshFilter source, Mesh mesh)
        {
            if (source == null) { DestroyTemp(mesh); return; }
            MeshFilter copy = null;
            foreach (var (s, c) in _helixLayers) if (s == source) copy = c;
            if (copy == null)
            {
                string name = source.name + "Helix";
                var parent = source.transform.parent;
                var existing = parent != null ? parent.Find(name) : null;
                var go = existing != null ? existing.gameObject : new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(parent, false);
                go.transform.SetLocalPositionAndRotation(source.transform.localPosition, source.transform.localRotation);
                go.transform.localScale = source.transform.localScale;
                copy = go.GetComponent<MeshFilter>();
                _helixLayers.Add((source, copy));
            }
            copy.gameObject.SetActive(source.gameObject.activeSelf);
            var from = source.GetComponent<MeshRenderer>();
            var to = copy.GetComponent<MeshRenderer>();
            if (from != null && to != null)
            {
                to.sharedMaterial = from.sharedMaterial;
                to.sortingLayerID = from.sortingLayerID;
                to.sortingOrder = from.sortingOrder;
                to.enabled = from.enabled;
            }
            if (copy.sharedMesh != null) DestroyTemp(copy.sharedMesh);
            copy.sharedMesh = mesh;
        }

        // Sand strips from the kerb outwards in the corners: wide on the outside (where a car that runs wide
        // ends up), narrower on the inside, none on straights. U runs 0 at the kerb to 1 at the outer edge.
        bool HiddenAt(float distance)
        {
            foreach (var gap in track.Layout.gaps)
                if (gap.hidden && track.Path.DeltaDistance(gap.from, distance) >= 0f && track.Path.DeltaDistance(distance, gap.to) >= 0f) return true;
            return false;
        }

        Mesh BuildRunoff(TrackPath path)
        {
            int n = path.Count;
            var widths = new float[2, n];
            for (int i = 0; i < n; i++)
            {
                float turn = path.TurnAt(i, runoffSpan);
                float extra = runoffCorner * Mathf.SmoothStep(0f, 1f, Mathf.Abs(turn) / runoffFullTurn);
                float inside = extra * runoffInside / Mathf.Max(0.01f, runoffCorner);
                widths[0, i] = turn < 0f ? extra : inside; // left side is outside in a right turn
                widths[1, i] = turn > 0f ? extra : inside;
                if (track.InRoom(path[i])) widths[0, i] = widths[1, i] = 0f; // indoors the floor runs up to the kerb
                if (HiddenAt(path.DistanceAt(i))) widths[0, i] = widths[1, i] = 0f; // the unraced way back: nothing drawn
            }
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? 1f : -1f;
                int start = vertices.Count;
                for (int i = 0; i <= n; i++)
                {
                    // Smooth the width along the road so the sand flows in and out of corners.
                    float w = 0f;
                    for (int k = -4; k <= 4; k++) w += widths[side, path.Wrap(i + k)];
                    w /= 9f;
                    if (w < 0.05f) w = 0f;
                    Vector2 p = path[i], normal = path.SampleNormal(i) * sign;
                    float v = (i == n ? path.Length : path.DistanceAt(i)) / runoffTile;
                    float kerbEdge = track.OffRoadAt(i == n ? path.Length : path.DistanceAt(i));
                    vertices.Add(p + normal * (kerbEdge - 0.05f));
                    vertices.Add(p + normal * (kerbEdge + w));
                    uvs.Add(new Vector2(0f, v));
                    uvs.Add(new Vector2(0.96f, v));
                    if (i == n) break;
                    int a = start + i * 2;
                    if (side == 0) triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                    else triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
            return TrackMeshes.Quads("Runoff", vertices, uvs, triangles);
        }

        // One open box per grid slot (same placement as Track.GridSlot): a bar across in front of the car
        // and two short bars along its sides, open toward the back.
        Mesh BuildGrid(TrackPath path, TrackLayout layout)
        {
            const float bar = 0.16f, halfWidth = 0.95f, front = 1.35f, sideLength = 1.1f;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i < gridSlots; i++)
            {
                float distance = -(3f + i * 2.6f);
                float lateral = (i % 2 == 0 ? 1f : -1f) * layout.roadWidth * 0.22f;
                Vector2 forward = path.TangentAt(distance), side = path.NormalAt(distance);
                Vector2 centre = path.PointAt(distance) + side * lateral;
                Vector2 frontCentre = centre + forward * front;
                TrackMeshes.AddQuad(vertices, uvs, triangles, frontCentre, side * (halfWidth + bar * 0.5f), forward * (bar * 0.5f), Vector2.one);
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector2 c = frontCentre + side * (k * halfWidth) - forward * (sideLength * 0.5f);
                    TrackMeshes.AddQuad(vertices, uvs, triangles, c, side * (bar * 0.5f), forward * (sideLength * 0.5f), Vector2.one);
                }
            }
            return TrackMeshes.Quads("Grid", vertices, uvs, triangles);
        }

        Mesh BuildStartLine(TrackPath path, float hw)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            Vector2 forward = path.TangentAt(0f), normal = path.NormalAt(0f);
            TrackMeshes.AddQuad(vertices, uvs, triangles, path.PointAt(0f), normal * hw, forward * 0.5f,
                new Vector2(hw, 1f));
            return TrackMeshes.Quads("StartLine", vertices, uvs, triangles);
        }

        // A grid so the grass can carry large, soft light and dark meadow patches in its vertex colours.
        Mesh BuildGrass(Rect area, bool water, bool snow)
        {
            const float cell = 3f;
            int nx = Mathf.CeilToInt(area.width / cell), ny = Mathf.CeilToInt(area.height / cell);
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            for (int j = 0; j <= ny; j++)
                for (int i = 0; i <= nx; i++)
                {
                    var p = new Vector2(area.xMin + i * cell, area.yMin + j * cell);
                    vertices.Add(p);
                    float tile = water ? waterTile : grassTile;
                    uvs.Add(new Vector2(p.x / tile, p.y / tile));
                    float t = Mathf.PerlinNoise(p.x * 0.035f + 100f, p.y * 0.035f) * 0.7f + Mathf.PerlinNoise(p.x * 0.11f, p.y * 0.11f + 50f) * 0.3f;
                    colors.Add(water ? Color.Lerp(new Color(0.9f, 0.95f, 1f), Color.white, t)
                        : snow ? Color.Lerp(new Color(0.88f, 0.92f, 0.98f), Color.white, Mathf.SmoothStep(0f, 1f, t)) // soft blue drifts
                        : Color.Lerp(meadowDark, meadowLight, Mathf.SmoothStep(0f, 1f, t)));
                }
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
            return TrackMeshes.Quads("Grass", vertices, uvs, triangles, colors);
        }

        static Rect Expand(Rect r, float margin) =>
            Rect.MinMaxRect(r.xMin - margin, r.yMin - margin, r.xMax + margin, r.yMax + margin);

        static void DestroyTemp(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
