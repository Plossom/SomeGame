using System.Collections.Generic;
using SomeGame.Car;
using SomeGame.Race;
using Unity.Netcode;
using UnityEngine;

namespace SomeGame.Online
{
    /// <summary>
    /// An online race on this phone: puts every room member on the grid (seat = lobby slot) in their
    /// colour, drives your car (and, on the host, the bots) and sends them to the room about 20 times a
    /// second; the other cars are puppets that follow what the room sends. Added by the RaceManager.
    /// </summary>
    public class OnlineRace : MonoBehaviour
    {
        const float SendInterval = 0.05f;

        public class Entry
        {
            public int Slot;
            public string Name;
            public Color Color;
            public CarMovement Car;
            public RaceProgress Progress;
            public NetCarPuppet Puppet; // null for cars driven on this phone
            public bool Bot;
        }

        RaceManager _race;
        OnlineRoom _room;
        readonly List<Entry> _entries = new();
        readonly Dictionary<int, Entry> _bySlot = new();
        float _sendTimer;
        bool _reported;

        public IReadOnlyList<Entry> Entries => _entries;
        public Entry EntryOf(RaceProgress progress) => _entries.Find(e => e.Progress == progress);

        /// <summary>Seconds until GO (infinite while the room still waits for a phone to load).</summary>
        public float SecondsToGo
        {
            get
            {
                if (_room == null || NetworkManager.Singleton == null) return 0f; // the room is gone: just go
                double go = _room.GoTime.Value;
                return go <= 0 ? float.PositiveInfinity : (float)(go - NetworkManager.Singleton.ServerTime.Time);
            }
        }

        public bool WaitingForPlayers => _room != null && _room.GoTime.Value <= 0;
        public bool IsHost => _room != null && _room.IsServer;
        public OnlineRoom Room => _room;

        /// <summary>Seats the room's cars. Returns this phone's car.</summary>
        public RaceProgress Setup(RaceManager race, CarMovement player, List<CarMovement> rivals)
        {
            _race = race;
            _room = OnlineRoom.Current;
            int me = _room.LocalSlot;
            var spare = new Queue<CarMovement>(rivals);
            RaceProgress mine = null;
            for (int slot = 0; slot < _room.Slots.Count; slot++)
            {
                var seat = _room.Slots[slot];
                if (seat.Kind == SlotKind.Empty || !_room.InRace(slot)) continue;
                bool local = slot == me;
                if (!local && spare.Count == 0) continue;
                var car = local ? player : spare.Dequeue();
                var entry = new Entry
                {
                    Slot = slot,
                    Name = seat.Name.ToString(),
                    Color = OnlineColors.Get(seat.Color),
                    Car = car,
                    Bot = seat.Kind == SlotKind.Bot,
                };
                Paint(car, entry.Color);
                if (!local)
                {
                    if (entry.Bot && _room.IsServer)
                    {
                        // The host drives the bots with the usual rival AI.
                        race.ScaleRival(car);
                        if (car.TryGetComponent(out SomeGame.Input.AIDriverInput ai))
                        {
                            ai.Adventurous = Mathf.Lerp(0.2f, 0.85f, slot / 7f);
                            ai.Seed = slot * 7919 + 13;
                        }
                    }
                    else
                    {
                        if (car.TryGetComponent(out SomeGame.Input.AIDriverInput ai)) ai.enabled = false;
                        if (car.TryGetComponent(out CarTerrain terrain)) terrain.enabled = false; // splashes, ramps... happen on its own phone
                        car.MakePuppet();
                        entry.Puppet = car.gameObject.AddComponent<NetCarPuppet>();
                    }
                    var tag = new GameObject("NameTag").AddComponent<NameTag>();
                    tag.Follow(car.transform, entry.Name, entry.Color);
                }
                if (local && OnlineDev.AutoDrive) car.gameObject.AddComponent<OnlineAutoPilot>();
                entry.Progress = race.Register(car, slot, local ? "You" : entry.Name);
                entry.Progress.Remote = entry.Puppet != null;
                if (local) mine = entry.Progress;
                _entries.Add(entry);
                _bySlot[slot] = entry;
            }
            while (spare.Count > 0) spare.Dequeue().gameObject.SetActive(false);
            if (mine == null)
            {
                // Not seated (should not happen): watch from the back of the grid.
                mine = race.Register(player, 7, "You");
                player.gameObject.SetActive(false);
            }
            _room.StateReceived += OnState;
            _room.Changed += OnRoomChanged;
            return mine;
        }

        void OnDestroy()
        {
            if (_room == null) return;
            _room.StateReceived -= OnState;
            _room.Changed -= OnRoomChanged;
        }

        public void ReportLoaded()
        {
            if (_reported || _room == null) return;
            _reported = true;
            _room.ReportLoaded();
        }

        static void Paint(CarMovement car, Color color)
        {
            var visual = Visual(car);
            if (visual != null) visual.color = color;
        }

        /// <summary>The car's body sprite (the one in the car's colour).</summary>
        public static SpriteRenderer Visual(CarMovement car)
        {
            foreach (var r in car.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.name == "Visual") return r;
            return null;
        }

        // Someone left during the race: their car parks and fades away.
        void OnRoomChanged()
        {
            if (_room == null) return;
            foreach (var e in _entries)
            {
                if (e.Puppet == null || !e.Car.gameObject.activeSelf) continue;
                var seat = _room.Slots[e.Slot];
                if (seat.Kind == SlotKind.Empty) e.Puppet.Vanish();
            }
        }

        void OnState(CarState state)
        {
            if (!_bySlot.TryGetValue(state.Slot, out var e) || e.Puppet == null) return;
            e.Puppet.Push(state);
            if (_race.State != RaceState.Countdown)
                e.Progress.ApplyRemote(state.Checkpoints, state.Has(CarState.Finished), state.FinishTime, _race.StartTime);
        }

        void Update()
        {
            if (_room == null || !_room.IsSpawned) return;
            _sendTimer -= Time.unscaledDeltaTime;
            if (_sendTimer > 0f) return;
            _sendTimer = SendInterval;
            foreach (var e in _entries)
                if (e.Puppet == null && e.Car.gameObject.activeSelf) _room.Submit(Snapshot(e));
        }

        CarState Snapshot(Entry e)
        {
            var car = e.Car;
            var sensor = car.GetComponent<SomeGame.Track.TrackSensor>();
            byte flags = 0;
            if (car.IsBoosting) flags |= CarState.Boost;
            if (sensor != null && sensor.Elevated) flags |= CarState.Elevated;
            if (car.IsAirborne) flags |= CarState.Airborne;
            if (e.Progress.IsFinished) flags |= CarState.Finished;
            var visual = Visual(car);
            if (visual != null && !visual.enabled) flags |= CarState.Hidden; // blinking after a splash
            return new CarState
            {
                Slot = (byte)e.Slot,
                Position = car.Body.position,
                Rotation = car.Body.rotation,
                Velocity = car.Body.linearVelocity,
                Drift = (sbyte)car.DriftDirection,
                Tier = (byte)car.DriftTier,
                Air = (byte)Mathf.RoundToInt(Mathf.Clamp01(car.AirHeight) * 255f),
                Flags = flags,
                Checkpoints = (ushort)Mathf.Max(0, e.Progress.CheckpointsPassed),
                FinishTime = e.Progress.IsFinished ? e.Progress.FinishTime - _race.StartTime : 0f,
            };
        }
    }
}
