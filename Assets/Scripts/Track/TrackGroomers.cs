using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Snowcats grooming the piste (<see cref="TrackLayout.groomers"/>): big orange machines crawling down the road at their
    /// own pace, side by side and staggered, switching lanes now and then (their indicators blink first). They are solid and
    /// heavy - bounce off their back, get flung aside by their blade. They come out of a snow tunnel at the top of their
    /// stretch and disappear into one at the bottom.
    /// </summary>
    public class TrackGroomers : TrackDerivedBehaviour, Track.IHazard
    {
        [SerializeField] Material plain;

        const int ShadowOrder = 7, BodyOrder = 9, LightOrder = 10;
        const float Length = 5.2f, Width = 3.4f, LaneTime = 6.5f, Switch = 1.6f, Blink = 1.3f;

        static readonly Color Orange = MeshKit.Hex("F07A22"), OrangeDark = MeshKit.Hex("B9551A"), Glass = MeshKit.Hex("2D3A48"),
            GlassLight = MeshKit.Hex("6F8AA6"), Belt = MeshKit.Hex("2A2E35"), Tread = MeshKit.Hex("4A505A"), Blade = MeshKit.Hex("C8CED8"),
            BladeDark = MeshKit.Hex("8A929F"), Tiller = MeshKit.Hex("5C6470"), Beacon = MeshKit.Hex("FFB020"), Amber = MeshKit.Hex("FFC23A"),
            Shadow = new Color(0.02f, 0.04f, 0.12f, 0.32f);

        class Cat
        {
            public TrackLayout.Groomers Data;
            public int Index;
            public Rigidbody2D Body;
            public Transform Root;
            public GameObject BlinkLeft, BlinkRight;
            public float Along, Lateral;
        }

        readonly List<Cat> _cats = new();
        readonly List<GameObject> _parts = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            if (track != null && !track.Hazards.Contains(this)) track.Hazards.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (track != null) track.Hazards.Remove(this);
        }

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            _cats.Clear();
            if (layout.groomers == null || layout.groomers.Count == 0) return;
            var root = new GameObject("Groomers") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            var body = BodyMesh();
            var left = BlinkMesh(-1f);
            var right = BlinkMesh(1f);
            foreach (var g in layout.groomers)
                for (int i = 0; i < g.count; i++)
                {
                    var go = new GameObject("Snowcat") { hideFlags = HideFlags.DontSave };
                    go.transform.SetParent(root, false);
                    MeshKit.Part(go.transform, "Body", body, plain, BodyOrder, out _);
                    var l = MeshKit.Part(go.transform, "BlinkL", left, plain, LightOrder, out _).gameObject;
                    var r = MeshKit.Part(go.transform, "BlinkR", right, plain, LightOrder, out _).gameObject;
                    var cat = new Cat { Data = g, Index = i, Root = go.transform, BlinkLeft = l, BlinkRight = r };
                    if (Application.isPlaying)
                    {
                        cat.Body = go.AddComponent<Rigidbody2D>();
                        cat.Body.bodyType = RigidbodyType2D.Kinematic;
                        cat.Body.interpolation = RigidbodyInterpolation2D.Interpolate;
                        var box = go.AddComponent<BoxCollider2D>();
                        box.size = new Vector2(Width, Length);
                        go.AddComponent<TrackGroomerBody>();
                    }
                    _cats.Add(cat);
                }
            Place(Application.isPlaying ? Track.Clock : 0f, true);
        }

        // An orange snowcat seen from above, pointing up (+y): tracks, body, cab, beacon, blade in front, tiller behind.
        Mesh BodyMesh()
        {
            var kit = new MeshKit();
            float hw = Width * 0.5f, hl = Length * 0.5f;
            kit.Quad(new Vector2(-hw + 0.5f, -hl - 0.6f), new Vector2(hw + 0.7f, -hl - 0.6f), new Vector2(hw + 0.7f, hl - 0.2f), new Vector2(-hw + 0.5f, hl - 0.2f), Shadow);
            foreach (float s in new[] { -1f, 1f })
            {
                kit.Quad(new Vector2(s * hw, -hl), new Vector2(s * (hw - 0.9f), -hl), new Vector2(s * (hw - 0.9f), hl), new Vector2(s * hw, hl), Belt);
                for (float y = -hl + 0.2f; y < hl; y += 0.45f)
                    kit.Quad(new Vector2(s * hw, y), new Vector2(s * (hw - 0.9f), y), new Vector2(s * (hw - 0.9f), y + 0.15f), new Vector2(s * hw, y + 0.15f), Tread);
            }
            kit.Quad(new Vector2(-hw + 0.85f, -hl + 0.3f), new Vector2(hw - 0.85f, -hl + 0.3f), new Vector2(hw - 0.85f, hl - 0.3f), new Vector2(-hw + 0.85f, hl - 0.3f), Orange);
            kit.Quad(new Vector2(-hw + 0.85f, -hl + 0.3f), new Vector2(hw - 0.85f, -hl + 0.3f), new Vector2(hw - 0.85f, -hl + 1.2f), new Vector2(-hw + 0.85f, -hl + 1.2f), OrangeDark);
            // the cab: dark glass all round, an orange roof with the beacon painted on
            kit.Quad(new Vector2(-0.95f, 0f), new Vector2(0.95f, 0f), new Vector2(0.95f, 1.9f), new Vector2(-0.95f, 1.9f), Glass);
            kit.Quad(new Vector2(-0.8f, 1.5f), new Vector2(0.8f, 1.5f), new Vector2(0.8f, 1.8f), new Vector2(-0.8f, 1.8f), GlassLight);
            kit.Quad(new Vector2(-0.75f, 0.15f), new Vector2(0.75f, 0.15f), new Vector2(0.75f, 1.35f), new Vector2(-0.75f, 1.35f), Orange);
            kit.Disc(new Vector2(0f, 0.75f), 0.28f, Beacon, 12);
            // the blade in front (wider than the machine), the tiller behind
            kit.Quad(new Vector2(-hw - 0.35f, hl + 0.1f), new Vector2(hw + 0.35f, hl + 0.1f), new Vector2(hw + 0.1f, hl + 0.75f), new Vector2(-hw - 0.1f, hl + 0.75f), Blade, Blade, BladeDark, BladeDark);
            kit.Quad(new Vector2(-hw + 0.3f, -hl - 0.15f), new Vector2(hw - 0.3f, -hl - 0.15f), new Vector2(hw - 0.3f, -hl - 0.85f), new Vector2(-hw + 0.3f, -hl - 0.85f), Tiller);
            return kit.Build("Snowcat");
        }

        Mesh BlinkMesh(float side)
        {
            var kit = new MeshKit();
            float hw = Width * 0.5f, hl = Length * 0.5f;
            kit.Disc(new Vector2(side * (hw - 0.4f), hl - 0.4f), 0.24f, Amber, 10);
            kit.Disc(new Vector2(side * (hw - 0.4f), -hl + 0.5f), 0.24f, Amber, 10);
            return kit.Build("SnowcatBlink");
        }

        // Where a snowcat is at a time: how far down its stretch, which lane, and which way it is about to switch (blinking).
        void Pose(Cat c, float time, out float along, out float lateral, out int blink)
        {
            var g = c.Data;
            float span = track.Path.DeltaDistance(g.from, g.to);
            float stagger = c.Index * span / Mathf.Max(1, g.count) + Track.SeedShift(g.from + c.Index) * 12f;
            along = g.from + Mathf.Repeat(time * g.speed + stagger, span);
            int Lane(int k) => Mathf.FloorToInt(MeshKit.Hash(c.Index * 13 + k, g.from) * g.lanes.Length * 0.9999f);
            float phase = time + c.Index * 2.3f;
            int slot = Mathf.FloorToInt(phase / LaneTime);
            float into = phase - slot * LaneTime;
            float now = g.lanes[Lane(slot)], before = g.lanes[Lane(slot - 1)];
            lateral = Mathf.Lerp(before, now, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(into / Switch)));
            float next = g.lanes[Lane(slot + 1)];
            blink = LaneTime - into < Blink && Mathf.Abs(next - now) > 0.1f ? (next > now ? 1 : -1) : 0;
        }

        void Place(float time, bool snap)
        {
            var path = track.Path;
            foreach (var c in _cats)
            {
                if (c.Root == null) continue;
                Pose(c, time, out float along, out float lateral, out int blink);
                Vector2 p = path.PointAt(along) + path.NormalAt(along) * lateral, t = path.TangentAt(along);
                float angle = Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg - 90f;
                bool jumped = Mathf.Abs(along - c.Along) > 20f; // back to the top of its stretch (inside the tunnel)
                c.Along = along;
                c.Lateral = lateral;
                if (c.Body != null && !snap && !jumped)
                {
                    c.Body.MovePosition(p);
                    c.Body.MoveRotation(angle);
                }
                else
                {
                    if (c.Body != null) { c.Body.position = p; c.Body.rotation = angle; }
                    c.Root.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, angle));
                }
                bool on = Mathf.Repeat(time * 3f, 1f) < 0.5f;
                c.BlinkLeft.SetActive(blink > 0 && on);  // + lateral is left
                c.BlinkRight.SetActive(blink < 0 && on);
            }
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying || _cats.Count == 0) return;
            Place(Track.Clock, false);
        }

        void Update()
        {
            if (Application.isPlaying || _cats.Count == 0) return;
            Place(0f, true);
        }

        // ------------------------------------------------------------------ hazard

        public bool VoidAt(Vector2 p, TrackPoint near) => false;

        public void Guide(CarMovement car, float skill, float distance, float ahead, ref float lateral, ref float speed)
        {
            var path = track.Path;
            float v = Mathf.Max(4f, car.ForwardSpeed);
            foreach (var c in _cats)
            {
                float gap = path.DeltaDistance(distance, c.Along) - Length * 0.5f - 1.5f;
                if (gap < -Length || gap > 34f) continue;
                // where it will be when this rival catches it
                float catchUp = Mathf.Max(0.1f, v - c.Data.speed);
                Pose(c, Track.Clock + Mathf.Clamp(gap / catchUp, 0f, 3f), out _, out float lat, out _);
                float clear = Width * 0.5f + 1.6f + (1f - skill) * -0.6f; // a careless rival squeezes by too close
                if (Mathf.Abs(lateral - lat) >= clear) continue;
                float limit = track.HalfWidthAt(c.Along) - 0.9f;
                float left = lat + clear, right = lat - clear;
                bool leftOk = left <= limit, rightOk = right >= -limit;
                // another snowcat in the way on that side?
                foreach (var o in _cats)
                {
                    if (o == c || Mathf.Abs(path.DeltaDistance(c.Along, o.Along)) > Length + 3f) continue;
                    if (Mathf.Abs(o.Lateral - left) < clear) leftOk = false;
                    if (Mathf.Abs(o.Lateral - right) < clear) rightOk = false;
                }
                if (leftOk || rightOk)
                {
                    float dodge = leftOk && (!rightOk || Mathf.Abs(left - lateral) < Mathf.Abs(right - lateral)) ? left : right;
                    lateral = Mathf.Lerp(lateral, dodge, Mathf.InverseLerp(34f, 8f, gap));
                }
                else speed = Mathf.Min(speed, c.Data.speed + Mathf.Max(0f, gap) * 0.5f);
            }
        }
    }
}
