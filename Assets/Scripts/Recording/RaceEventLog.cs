#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SomeGame.Car;
using SomeGame.Race;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Recording
{
    /// <summary>
    /// Watches the player's car through a recorded run and notes what happens, for cutting the video: every event has
    /// t (seconds into the video, where to cut), race_t (seconds since GO; slow motion stretches t, not race_t),
    /// type, and at (lap distance in metres). Everything is what the car actually did.
    /// </summary>
    public class RaceEventLog : MonoBehaviour
    {
        const float MinImpact = 2f;         // softer touches are no crash
        const float CrashRepeat = 0.75f;    // scraping along one wall is one crash
        const float NearRange = 3.5f;       // what is watched for a near miss
        const float NearGap = 1f;           // came this close (metres of air between them)
        const float NearSpeed = 12f;        // at this speed at least (relative to a moving thing: NearCarSpeed)
        const float NearCarSpeed = 3f;
        const float NearMaxSize = 6f;       // the long walls beside the road are always near
        const float OvertakeGrace = 3f;     // places shuffle as the grid gets going

        readonly struct Note
        {
            public readonly float T, RaceT, At;
            public readonly string Type;
            public readonly (string key, object value)[] Fields;
            public Note(float t, float raceT, float at, string type, (string, object)[] fields) { T = t; RaceT = raceT; At = at; Type = type; Fields = fields; }
        }

        class Close { public string Name; public float Gap, Speed; public int Seen; public bool Hit; }

        readonly List<Note> _notes = new();
        readonly Dictionary<object, Close> _close = new();
        readonly Dictionary<string, float> _crashedAt = new();
        readonly HashSet<int> _forks = new();
        readonly List<Collider2D> _found = new();

        RaceManager _race;
        CarMovement _car;
        RaceProgress _progress;
        TrackSensor _sensor;
        Collider2D _collider;
        string _video, _title, _camera;
        int _raceNumber, _run, _seed, _startFrame = -1, _place, _sections;
        bool _slowMotion, _finished;
        float _launchedAt, _clockStartedAt;

        public string Summary => $"{(_finished ? "finished P" + _race.FinalPosition + " in " + _race.RaceTime.ToString("F1") + " s" : "did not finish")}, {_notes.Count} events";

        float VideoTime => _startFrame < 0 ? 0f : (Time.frameCount - _startFrame) / (float)ShortsOptions.FrameRate;

        public void Init(RaceManager race, CarMovement car, string video, string title, int raceNumber, int run, int seed, ShortsOptions options)
        {
            _race = race;
            _car = car;
            _progress = car.GetComponent<RaceProgress>();
            _sensor = car.GetComponent<TrackSensor>();
            _collider = car.GetComponent<Collider2D>();
            _video = video;
            _title = title;
            _raceNumber = raceNumber;
            _run = run;
            _seed = seed;
            _camera = options.camera.ToString();
            _slowMotion = options.slowMotion;
            race.RaceStarted += OnRaceStarted;
            race.PlayerFinished += OnFinished;
            _progress.LapCompleted += OnLap;
            car.Launched += OnLaunched;
            car.Landed += OnLanded;
            car.Slipped += OnSlipped;
            car.DriftStarted += OnDrift;
            car.CarryStarted += OnCarried;
            var terrain = car.GetComponent<CarTerrain>();
            terrain.Fell += OnFell;
            terrain.Splashed += OnSplashed;
            terrain.PenguinHit += OnPenguin;
            terrain.CowHit += OnCow;
        }

        /// <summary>The recorder has started: the video's clock runs from here.</summary>
        public void StartClock() => _startFrame = Time.frameCount;

        public void Add(string type, params (string key, object value)[] fields) =>
            _notes.Add(new Note(VideoTime, _race.RaceTime, _sensor.Current.Distance, type, fields));

        void OnRaceStarted()
        {
            _place = _race.PositionOf(_progress);
            _sections = _race.Laps;
            Add("race_start", ("grid", _place), ("rivals", _race.Standings.Count - 1));
        }

        void OnLap(RaceProgress car, float seconds)
        {
            if (_progress.LapsCompleted >= _progress.TotalLaps) return; // that one is the finish
            Add(_race.Sectioned ? "section_complete" : "lap_complete", ("number", _progress.LapsCompleted), ("of", _sections),
                ("seconds", seconds), ("position", _race.PositionOf(_progress)));
        }

        void OnFinished()
        {
            _finished = true;
            Add("finish", ("position", _race.FinalPosition), ("seconds", _race.RaceTime));
        }

        void OnLaunched(float air)
        {
            _launchedAt = Time.time;
            Add("jump_start", ("air_time", air), ("speed", _car.ForwardSpeed));
        }

        void OnLanded() => Add("landing", ("air_time", Time.time - _launchedAt), ("speed", _car.ForwardSpeed));
        void OnSlipped() => Crash("oil puddle", "slip", 0f);
        void OnCarried() => Add("carried_back");
        void OnDrift() => Add("drift_start");
        void OnPenguin() => Crash("penguin", "hit", _car.ForwardSpeed);
        void OnCow(int index) => Crash("cow", "hit", _car.ForwardSpeed);
        void OnSplashed() => Crash(HoleHere() ?? "water", "fall", _car.ForwardSpeed);
        void OnFell() => Crash(HoleHere() ?? "the abyss", "fall", _car.ForwardSpeed);

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (_car.IsAirborne) return;
            object key = (object)collision.collider;
            if (_close.TryGetValue(key, out var close)) close.Hit = true;
            float impact = collision.relativeVelocity.magnitude;
            if (impact < MinImpact) return;
            var rival = collision.collider.GetComponentInParent<RaceProgress>();
            Crash(rival != null ? rival.DisplayName : Tidy(collision.collider.name), rival != null ? "car" : "scenery", impact);
        }

        void Crash(string what, string kind, float speed)
        {
            if (_crashedAt.TryGetValue(what, out float at) && Time.time - at < CrashRepeat) return;
            _crashedAt[what] = Time.time;
            Add("crash", ("object", what), ("kind", kind), ("speed", speed));
        }

        static string Tidy(string name) => name.Replace("(Clone)", "").Trim();

        // What the car went into: the hole nearest to it, if there is one.
        string HoleHere()
        {
            var layout = _sensor.Track.Layout;
            if (layout.holes == null) return null;
            Vector2 at = _car.Body.position;
            foreach (var hole in layout.holes)
                if ((HolePosition(hole) - at).magnitude < hole.radius + 2.5f) return HoleName(hole);
            return null;
        }

        Vector2 HolePosition(TrackLayout.Hole hole)
        {
            var path = _sensor.Track.Path;
            return path.PointAt(hole.distance) + path.NormalAt(hole.distance) * hole.lateral;
        }

        static string HoleName(TrackLayout.Hole hole) => hole.tub ? "hot tub" : "ice hole";

        void Update()
        {
            if (_startFrame < 0 || _race.State != RaceState.Racing) return;
            Forks();
            Overtakes();
            NearMisses();
        }

        // Which way the car went at a split, read off where it drives once it is on one of the forks.
        void Forks()
        {
            var track = _sensor.Track;
            var splits = track.Layout.splits;
            if (splits == null || _car.IsAirborne) return;
            float d = _sensor.Current.Distance, lateral = _sensor.Current.Lateral;
            for (int i = 0; i < splits.Count; i++)
            {
                if (_forks.Contains(i)) continue;
                var path = track.Path;
                if (path.DeltaDistance(splits[i].from, d) < 0f || path.DeltaDistance(d, splits[i].to) < 0f) continue;
                if (Mathf.Abs(lateral) <= track.SplitHalf(d)) continue;
                _forks.Add(i);
                Add("fork_choice", ("side", lateral > 0f ? "left" : "right"), ("fork", i + 1));
            }
        }

        void Overtakes()
        {
            int place = _race.PositionOf(_progress);
            if (_race.RaceTime < OvertakeGrace) { _place = place; return; }
            if (place == _place) return;
            var standings = _race.Standings;
            if (place < _place && place < standings.Count) Add("overtake", ("rival", standings[place].DisplayName), ("position", place));
            else if (place > _place && place >= 2) Add("overtaken", ("rival", standings[place - 2].DisplayName), ("position", place));
            _place = place;
        }

        // Things passed close and fast without touching them: rivals and small solid things, and holes in the floor.
        void NearMisses()
        {
            int frame = Time.frameCount;
            if (!_car.IsAirborne && _race.RaceTime >= OvertakeGrace)
            {
                _found.Clear();
                Physics2D.OverlapCircle(_car.Body.position, NearRange, ContactFilter2D.noFilter, _found);
                foreach (var other in _found)
                {
                    if (other == _collider || other.isTrigger) continue;
                    var body = other.attachedRigidbody;
                    var rival = other.GetComponentInParent<RaceProgress>();
                    if (rival != null && rival.IsFinished) continue;
                    var size = other.bounds.size;
                    if (rival == null && Mathf.Max(size.x, size.y) > NearMaxSize) continue;
                    float speed = (_car.Body.linearVelocity - (body != null ? body.linearVelocity : Vector2.zero)).magnitude;
                    if (speed < (rival != null ? NearCarSpeed : NearSpeed)) continue;
                    Watch(other, rival != null ? rival.DisplayName : Tidy(other.name), other.Distance(_collider).distance, speed, frame);
                }
                var holes = _sensor.Track.Layout.holes;
                if (holes != null)
                    foreach (var hole in holes)
                    {
                        float gap = (HolePosition(hole) - _car.Body.position).magnitude - hole.radius;
                        if (gap < NearRange && _car.Body.linearVelocity.magnitude >= NearSpeed) Watch(hole, HoleName(hole), gap, _car.Body.linearVelocity.magnitude, frame);
                    }
            }
            List<object> gone = null;
            foreach (var (key, close) in _close)
            {
                if (close.Seen == frame) continue;
                (gone ??= new()).Add(key);
                if (!close.Hit && close.Gap <= NearGap && close.Gap > 0f) Add("near_miss", ("object", close.Name), ("gap", close.Gap), ("speed", close.Speed));
            }
            if (gone != null) foreach (var key in gone) _close.Remove(key);
        }

        void Watch(object thing, string name, float gap, float speed, int frame)
        {
            if (!_close.TryGetValue(thing, out var close)) _close[thing] = close = new Close { Name = name, Gap = gap, Speed = speed };
            close.Seen = frame;
            if (gap >= close.Gap) return;
            close.Gap = gap;
            close.Speed = speed;
        }

        public string ToJson()
        {
            var json = new StringBuilder("{\n");
            Field(json, "video", _video + ".mp4"); json.Append(", ");
            Field(json, "track", _title); json.Append(", ");
            Field(json, "race", _raceNumber); json.Append(", ");
            Field(json, "run", _run); json.Append(",\n");
            Field(json, "seed", _seed); json.Append(", ");
            Field(json, "fps", ShortsOptions.FrameRate); json.Append(", ");
            Field(json, "camera", _camera); json.Append(", ");
            Field(json, "slow_motion", _slowMotion); json.Append(",\n");
            Field(json, "finished", _finished); json.Append(", ");
            Field(json, "position", _race.FinalPosition); json.Append(", ");
            Field(json, "race_time", _race.RaceTime); json.Append(", ");
            Field(json, "video_length", VideoTime); json.Append(",\n\"events\": [\n");
            for (int i = 0; i < _notes.Count; i++)
            {
                var note = _notes[i];
                json.Append("  {");
                Field(json, "t", note.T);
                json.Append(", ");
                Field(json, "race_t", note.RaceT);
                json.Append(", ");
                Field(json, "type", note.Type);
                json.Append(", ");
                Field(json, "at", Mathf.Round(note.At));
                foreach (var (key, value) in note.Fields) { json.Append(", "); Field(json, key, value); }
                json.Append(i + 1 < _notes.Count ? "},\n" : "}\n");
            }
            return json.Append("]\n}\n").ToString();
        }

        static void Field(StringBuilder json, string key, object value)
        {
            json.Append('"').Append(key).Append("\": ");
            switch (value)
            {
                case float f: json.Append(f.ToString("0.###", CultureInfo.InvariantCulture)); break;
                case int n: json.Append(n); break;
                case bool b: json.Append(b ? "true" : "false"); break;
                default: json.Append('"').Append(value.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"'); break;
            }
        }

        void OnDestroy()
        {
            if (_race != null) { _race.RaceStarted -= OnRaceStarted; _race.PlayerFinished -= OnFinished; }
            if (_progress != null) _progress.LapCompleted -= OnLap;
        }
    }
}
#endif
