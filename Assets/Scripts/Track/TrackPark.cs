using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The snow park: kickers as real snow table tops (take-off, deck, landing - for gaps marked
    /// <see cref="TrackLayout.Gap.kicker"/>), blue safety nets along the piste (<see cref="TrackLayout.nets"/>), snow
    /// tunnels with red arches you drive under (<see cref="TrackLayout.snowTunnels"/>), and chairlifts overhead with
    /// their chairs going round (<see cref="TrackLayout.chairlifts"/>).
    /// </summary>
    public class TrackPark : TrackDerivedBehaviour
    {
        [SerializeField] Material plain;

        const int MoundOrder = -43, NetOrder = -41, TunnelOrder = 60, LiftShadowOrder = -39, LiftOrder = 70, ChairOrder = 71;
        static readonly Color SnowLit = new Color(0.99f, 0.99f, 1f).linear, SnowMid = new Color(0.86f, 0.9f, 0.97f).linear,
            SnowShade = new Color(0.7f, 0.77f, 0.88f).linear, Lip = new Color(0.5f, 0.6f, 0.75f).linear,
            Net = new Color(0.12f, 0.45f, 0.85f).linear, NetDark = new Color(0.08f, 0.3f, 0.62f).linear, Pole = new Color(0.9f, 0.92f, 0.95f).linear,
            Red = new Color(0.85f, 0.15f, 0.12f).linear, RedDark = new Color(0.6f, 0.08f, 0.06f).linear,
            Steel = new Color(0.32f, 0.34f, 0.38f).linear, Cable = new Color(0.12f, 0.12f, 0.14f).linear,
            Seat = new Color(0.95f, 0.55f, 0.1f).linear, Shade = new Color(0f, 0.05f, 0.15f, 0.25f),
            Paint = new Color(0.15f, 0.42f, 0.88f).linear, Shadow = new Color(0.05f, 0.12f, 0.3f, 0.3f);

        readonly List<GameObject> _parts = new();
        readonly List<(Mesh mesh, Color[] colors, TrackLayout.Span span)> _tunnels = new();
        readonly List<(TrackPath line, List<Transform> chairs, List<Transform> shadows)> _lifts = new();
        Transform _player;
        float _tunnelAlpha = 1f;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _tunnels.Clear(); _lifts.Clear();
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            var sv = new List<Vector3>(); var sc = new List<Color>(); var st = new List<int>();
            for (int g = 0; g < layout.gaps.Count; g++) if (layout.gaps[g].kicker) Mound(path, layout.gaps[g], v, c, t, sv, sc, st);
            if (sv.Count > 0) Part("KickerShadows", Mesh("KickerShadows", sv, sc, st), MoundOrder - 1, transform);
            if (v.Count > 0) Part("Kickers", Mesh("Kickers", v, c, t), MoundOrder, transform);
            v = new List<Vector3>(); c = new List<Color>(); t = new List<int>();
            if (layout.nets != null) foreach (var span in layout.nets) Nets(path, span, v, c, t);
            if (v.Count > 0) Part("Nets", Mesh("Nets", v, c, t), NetOrder, transform);
            if (layout.snowTunnels != null) foreach (var span in layout.snowTunnels) SnowTunnel(path, span);
            if (layout.chairlifts != null) foreach (var lift in layout.chairlifts) Chairlift(lift);
        }

        // ------------------------------------------------------------------ kickers

        // A table top across the kicker's width: the take-off ramp (shaded up to a hard lip), the flat deck, the landing.
        void Mound(TrackPath path, TrackLayout.Gap gap, List<Vector3> v, List<Color> c, List<int> t, List<Vector3> sv, List<Color> sc, List<int> st)
        {
            float half = gap.width > 0f ? gap.width * 0.5f : track.OffRoadAt(gap.from);
            float ramp = track.Layout.rampLength + 1.5f, deck = path.DeltaDistance(gap.from, gap.to), land = 6f;
            float start = gap.from - ramp, total = ramp + deck + land;
            for (float s = 0f; s < total; s += 1f)
            {
                float d0 = start + s, d1 = start + Mathf.Min(total, s + 1f);
                Color col0 = MoundShade(s, ramp, deck), col1 = MoundShade(Mathf.Min(total, s + 1f), ramp, deck);
                Vector2 a = path.PointAt(d0) + path.NormalAt(d0) * gap.rampLateral, b = path.PointAt(d1) + path.NormalAt(d1) * gap.rampLateral;
                Vector2 n0 = path.NormalAt(d0), n1 = path.NormalAt(d1);
                // a little wider at the deck (its shoulders), the side walls in shade
                float w0 = half * Shoulder(s, ramp, deck, land), w1 = half * Shoulder(s + 1f, ramp, deck, land);
                Quad(a - n0 * w0, b - n1 * w1, b + n1 * w1, a + n0 * w0, col0, col1, col1, col0, v, c, t);
                Quad(a + n0 * w0, b + n1 * w1, b + n1 * (w1 + 0.9f), a + n0 * (w0 + 0.9f), SnowShade, SnowShade, SnowShade, SnowShade, v, c, t);
                Quad(a - n0 * (w0 + 0.9f), b - n1 * (w1 + 0.9f), b - n1 * w1, a - n0 * w0, SnowMid, SnowMid, SnowMid, SnowMid, v, c, t);
            }
            // the lip and the knuckle (where the deck drops to the landing): painted blue lines across, like a real park
            foreach (float at in new[] { gap.from, gap.to })
            {
                Vector2 l = path.PointAt(at) + path.NormalAt(at) * gap.rampLateral, ln = path.NormalAt(at), lt = path.TangentAt(at);
                Quad(l - ln * half - lt * 0.3f, l - ln * half + lt * 0.3f, l + ln * half + lt * 0.3f, l + ln * half - lt * 0.3f, Paint, Paint, Paint, Paint, v, c, t);
            }
            // its shadow, thrown down and to the side over the landing
            for (float s = ramp; s < ramp + deck + land; s += 1f)
            {
                float d0 = start + s, d1 = d0 + 1f, fade = 1f - Mathf.Clamp01((s - ramp - deck) / land);
                Vector2 a = path.PointAt(d0) + path.NormalAt(d0) * gap.rampLateral, b = path.PointAt(d1) + path.NormalAt(d1) * gap.rampLateral;
                Vector2 n0 = path.NormalAt(d0), n1 = path.NormalAt(d1), off = new Vector2(1.4f, -1.8f) * fade;
                var sh = new Color(Shadow.r, Shadow.g, Shadow.b, Shadow.a * fade);
                Quad(a - n0 * half + off, b - n1 * half + off, b + n1 * half + off, a + n0 * half + off, sh, sh, sh, sh, sv, sc, st);
            }
        }

        static Color MoundShade(float s, float ramp, float deck)
        {
            if (s <= ramp) return Color.Lerp(SnowShade, SnowLit, s / ramp);      // climbing the take-off
            if (s <= ramp + deck) return SnowLit;                                 // the deck
            return Color.Lerp(SnowShade, SnowMid, Mathf.Clamp01((s - ramp - deck) / 6f)); // down the landing
        }

        static float Shoulder(float s, float ramp, float deck, float land) =>
            s < ramp ? 0.85f + 0.15f * s / ramp : s < ramp + deck ? 1f : Mathf.Lerp(1f, 0.85f, Mathf.Clamp01((s - ramp - deck) / land));

        // ------------------------------------------------------------------ nets

        // Blue safety netting along both sides of the piste, white poles every few metres.
        void Nets(TrackPath path, TrackLayout.Span span, List<Vector3> v, List<Color> c, List<int> t)
        {
            float length = path.DeltaDistance(span.from, span.to);
            foreach (float side in new[] { -1f, 1f })
            {
                for (float s = 0f; s < length; s += 1f)
                {
                    float d0 = span.from + s, d1 = span.from + Mathf.Min(length, s + 1f);
                    float o0 = track.OffRoadAt(d0) + 1.4f, o1 = track.OffRoadAt(d1) + 1.4f;
                    Vector2 a = path.PointAt(d0) + path.NormalAt(d0) * side * o0, b = path.PointAt(d1) + path.NormalAt(d1) * side * o1;
                    Vector2 n0 = path.NormalAt(d0) * side, n1 = path.NormalAt(d1) * side;
                    Color col = ((int)s / 2) % 2 == 0 ? Net : NetDark;
                    Quad(a - n0 * 0.18f, b - n1 * 0.18f, b + n1 * 0.18f, a + n0 * 0.18f, col, col, col, col, v, c, t);
                    // the net's shadow on the snow
                    Quad(a + n0 * 0.2f + new Vector2(0.3f, -0.4f), b + n1 * 0.2f + new Vector2(0.3f, -0.4f), b + n1 * 0.6f + new Vector2(0.3f, -0.4f), a + n0 * 0.6f + new Vector2(0.3f, -0.4f), Shade, Shade, Shade, Shade, v, c, t);
                    if ((int)s % 4 == 0) Quad(a - new Vector2(0.22f, 0.22f), a + new Vector2(0.22f, -0.22f), a + new Vector2(0.22f, 0.22f), a + new Vector2(-0.22f, 0.22f), Pole, Pole, Pole, Pole, v, c, t);
                }
            }
        }

        // ------------------------------------------------------------------ snow tunnels

        // A snow roof over the piste (over the cars), a red arch at each end; see-through while you are under it.
        void SnowTunnel(TrackPath path, TrackLayout.Span span)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            float length = path.DeltaDistance(span.from, span.to);
            for (float s = 0f; s < length; s += 1f)
            {
                float d0 = span.from + s, d1 = span.from + Mathf.Min(length, s + 1f);
                float w = track.OffRoadAt(d0) + 0.8f;
                Vector2 a = path.PointAt(d0), b = path.PointAt(d1), n0 = path.NormalAt(d0), n1 = path.NormalAt(d1);
                // the roof's curve: lit along its crown, shaded down its sides
                Quad(a - n0 * w, b - n1 * w, b, a, SnowShade, SnowShade, SnowLit, SnowLit, v, c, t);
                Quad(a, b, b + n1 * w, a + n0 * w, SnowLit, SnowLit, SnowMid, SnowMid, v, c, t);
            }
            foreach (float d in new[] { span.from, span.to })
            {
                float w = track.OffRoadAt(d) + 1.4f;
                Vector2 p = path.PointAt(d), n = path.NormalAt(d), tg = path.TangentAt(d);
                Quad(p - n * w - tg * 0.7f, p - n * w + tg * 0.7f, p + n * w + tg * 0.7f, p + n * w - tg * 0.7f, Red, Red, RedDark, RedDark, v, c, t);
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector2 foot = p + n * side * w;
                    Quad(foot - tg * 0.9f - n * 0.9f, foot + tg * 0.9f - n * 0.9f, foot + tg * 0.9f + n * 0.9f, foot - tg * 0.9f + n * 0.9f, RedDark, RedDark, RedDark, RedDark, v, c, t);
                }
            }
            var mesh = Mesh("SnowTunnel", v, c, t);
            Part("SnowTunnel", mesh, TunnelOrder, transform);
            _tunnels.Add((mesh, c.ToArray(), span));
        }

        // ------------------------------------------------------------------ chairlifts

        // Towers along the line, the cable, and chairs going round it (up one side, down the other), their shadows below.
        void Chairlift(TrackLayout.Chairlift lift)
        {
            if (lift.points == null || lift.points.Length < 2) return;
            var line = new TrackPath(lift.points, 1f);
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            var sv = new List<Vector3>(); var sc = new List<Color>(); var st = new List<int>();
            Vector2 so = new Vector2(3f, -4f);
            // (the loop the chairs ride is the closed path through the points; the cable is drawn along both sides of it)
            for (float d = 0f; d < line.Length; d += 1f)
            {
                Vector2 a = line.PointAt(d), b = line.PointAt(d + 1f);
                Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized;
                Quad(a - n * 0.09f, b - n * 0.09f, b + n * 0.09f, a + n * 0.09f, Cable, Cable, Cable, Cable, v, c, t);
                Quad(a - n * 0.06f + so, b - n * 0.06f + so, b + n * 0.06f + so, a + n * 0.06f + so, Shade, Shade, Shade, Shade, sv, sc, st);
            }
            for (int i = 0; i < lift.points.Length; i++)
            {
                Vector2 p = lift.points[i];
                Quad(p - new Vector2(0.6f, 0.6f), p + new Vector2(0.6f, -0.6f), p + new Vector2(0.6f, 0.6f), p + new Vector2(-0.6f, 0.6f), Steel, Steel, Steel, Steel, v, c, t);
                Vector2 dir = (lift.points[(i + 1) % lift.points.Length] - p).normalized, n = new Vector2(-dir.y, dir.x);
                Quad(p - n * 2.2f - dir * 0.25f, p - n * 2.2f + dir * 0.25f, p + n * 2.2f + dir * 0.25f, p + n * 2.2f - dir * 0.25f, Steel, Steel, Steel, Steel, v, c, t);
                Quad(p + so - new Vector2(0.4f, 0.4f), p + so + new Vector2(0.4f, -0.4f), p + so + new Vector2(0.4f, 0.4f), p + so + new Vector2(-0.4f, 0.4f), Shade, Shade, Shade, Shade, sv, sc, st);
            }
            var root = new GameObject("Chairlift") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            Part("LiftShadow", Mesh("LiftShadow", sv, sc, st), LiftShadowOrder, root);
            Part("LiftLine", Mesh("LiftLine", v, c, t), LiftOrder, root);
            var chairs = new List<Transform>(); var shadows = new List<Transform>();
            for (int k = 0; k < lift.chairs; k++)
            {
                var cv = new List<Vector3>(); var cc = new List<Color>(); var ct = new List<int>();
                Quad(new Vector2(-0.9f, -0.5f), new Vector2(0.9f, -0.5f), new Vector2(0.9f, 0.5f), new Vector2(-0.9f, 0.5f), Seat, Seat, Seat, Seat, cv, cc, ct);
                Quad(new Vector2(-0.9f, 0.35f), new Vector2(0.9f, 0.35f), new Vector2(0.9f, 0.6f), new Vector2(-0.9f, 0.6f), RedDark, RedDark, RedDark, RedDark, cv, cc, ct);
                var chair = PartObject("Chair", Mesh("Chair", cv, cc, ct), ChairOrder, root);
                chairs.Add(chair.transform);
                var sh = new List<Vector3>(); var shc = new List<Color>(); var sht = new List<int>();
                Quad(new Vector2(-0.8f, -0.4f), new Vector2(0.8f, -0.4f), new Vector2(0.8f, 0.4f), new Vector2(-0.8f, 0.4f), Shade, Shade, Shade, Shade, sh, shc, sht);
                shadows.Add(PartObject("ChairShadow", Mesh("ChairShadow", sh, shc, sht), LiftShadowOrder, root).transform);
            }
            _lifts.Add((line, chairs, shadows));
            PlaceChairs(0f);
        }

        void PlaceChairs(float time)
        {
            foreach (var (line, chairs, shadows) in _lifts)
                for (int k = 0; k < chairs.Count; k++)
                {
                    float d = Mathf.Repeat(time * 3.5f + k * line.Length / chairs.Count, line.Length);
                    Vector2 p = line.PointAt(d), dir = line.TangentAt(d);
                    var rot = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                    chairs[k].SetPositionAndRotation(p, rot);
                    shadows[k].SetPositionAndRotation(p + new Vector2(3f, -4f), rot);
                }
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            PlaceChairs(Track.Clock);
            if (_tunnels.Count == 0) return;
            if (_player == null)
            {
                var car = GameObject.Find("PlayerCar");
                if (car != null) _player = car.transform;
                if (_player == null) return;
            }
            // Under a snow tunnel: its roof all but vanishes, so you see your car.
            var sensor = _player.GetComponent<TrackSensor>();
            bool under = false;
            if (sensor != null)
                foreach (var (_, _, span) in _tunnels)
                    if (track.Path.DeltaDistance(span.from - 2f, sensor.Current.Distance) >= 0f && track.Path.DeltaDistance(sensor.Current.Distance, span.to + 2f) >= 0f) under = true;
            float alpha = Mathf.MoveTowards(_tunnelAlpha, under ? 0.25f : 1f, Time.deltaTime * 4f);
            if (alpha == _tunnelAlpha) return;
            _tunnelAlpha = alpha;
            foreach (var (mesh, colors, _) in _tunnels)
            {
                var cols = new Color[colors.Length];
                for (int i = 0; i < cols.Length; i++) { cols[i] = colors[i]; cols[i].a = colors[i].a * alpha; }
                mesh.colors = cols;
            }
        }

        // ------------------------------------------------------------------ helpers

        void Part(string name, Mesh mesh, int order, Transform parent) => PartObject(name, mesh, order, parent);

        GameObject PartObject(string name, Mesh mesh, int order, Transform parent)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = plain;
            renderer.sortingOrder = order;
            if (parent == transform) _parts.Add(go);
            return go;
        }

        static void Quad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color ca, Color cb, Color ccc, Color cd, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            c.Add(ca); c.Add(cb); c.Add(ccc); c.Add(cd);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
        }

        static Mesh Mesh(string name, List<Vector3> v, List<Color> c, List<int> t)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
