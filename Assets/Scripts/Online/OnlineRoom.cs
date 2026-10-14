using System;
using System.Collections.Generic;
using SomeGame.Race;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SomeGame.Online
{
    public enum RoomPhase : byte { Lobby, Loading, Racing }

    /// <summary>
    /// The online room, run by the host and mirrored on every phone: eight slots (players and bots, each
    /// with a name and a car colour no one else has), the chosen race and the race start. During a race
    /// it relays every car's state: each phone sends its own car to the host, the host sends all cars
    /// (its bots included) to everyone, about 20 times a second.
    /// </summary>
    public class OnlineRoom : NetworkBehaviour
    {
        public const int SlotCount = 8;
        const float SendInterval = 0.05f;
        const float LoadTimeout = 15f;
        /// <summary>Seconds from "everyone is ready" to GO: the countdown (3-2-1) plus a little margin.</summary>
        const float CountdownLead = 4.2f;

        public static OnlineRoom Current { get; private set; }

        public readonly NetworkList<LobbySlot> Slots = new();
        public readonly NetworkVariable<int> RaceIndex = new();
        public readonly NetworkVariable<RoomPhase> Phase = new();
        /// <summary>Server time of GO (0 until everyone has loaded the race).</summary>
        public readonly NetworkVariable<double> GoTime = new();
        /// <summary>Bit per slot: who takes part in the current race.</summary>
        public readonly NetworkVariable<byte> RaceMask = new();
        /// <summary>Counts the races of this room; states of an older race are ignored.</summary>
        public readonly NetworkVariable<int> RaceNumber = new();

        /// <summary>Anything shown in the lobby changed (slots, race, phase).</summary>
        public event Action Changed;
        /// <summary>A car's state arrived (on the host from the guests, on guests from the host).</summary>
        public event Action<CarState> StateReceived;

        readonly CarState?[] _latest = new CarState?[SlotCount];
        readonly HashSet<ulong> _loaded = new();
        float _loadingSince, _sendTimer;

        static readonly string[] BotNames = { "BOLT", "DASH", "ZOOM", "BLITZ", "SPARK", "COMET", "TURBO", "VROOM" };

        // ------------------------------------------------------------------ lifecycle

        public override void OnNetworkSpawn()
        {
            Current = this;
            DontDestroyOnLoad(gameObject);
            Slots.OnListChanged += _ => Changed?.Invoke();
            RaceIndex.OnValueChanged += (_, _) => Changed?.Invoke();
            Phase.OnValueChanged += OnPhaseChanged;
            if (IsServer)
            {
                for (int i = 0; i < SlotCount; i++) Slots.Add(default);
                RaceIndex.Value = Mathf.Clamp(PlayerPrefs.GetInt("Online.Race", 0), 0, Mathf.Max(0, Races.Count - 1));
                Seat(NetworkManager.LocalClientId, OnlineSession.PlayerName, OnlineSession.PreferredColor);
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }
            else HelloRpc(OnlineSession.PlayerName, (byte)OnlineSession.PreferredColor);
            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            if (Current == this) Current = null;
        }

        public override void OnDestroy()
        {
            if (Current == this) Current = null;
            base.OnDestroy();
        }

        // ------------------------------------------------------------------ queries

        /// <summary>Every race of every cup (with races), in map order.</summary>
        public static List<LevelDefinition> Races
        {
            get
            {
                var list = new List<LevelDefinition>();
                var cups = CupList.Instance;
                if (cups == null) return list;
                foreach (var cup in cups.cups)
                    if (cup.HasRaces)
                        foreach (var level in cup.races.levels)
                            if (level != null && !level.isTutorial) list.Add(level);
                return list;
            }
        }

        public LevelDefinition Race
        {
            get
            {
                var races = Races;
                return races.Count == 0 ? null : races[Mathf.Clamp(RaceIndex.Value, 0, races.Count - 1)];
            }
        }

        /// <summary>The slot of this phone's player, or -1.</summary>
        public int LocalSlot => SlotOf(NetworkManager.LocalClientId);

        public int SlotOf(ulong clientId)
        {
            for (int i = 0; i < Slots.Count; i++)
                if (Slots[i].Kind == SlotKind.Human && Slots[i].ClientId == clientId) return i;
            return -1;
        }

        public bool InRace(int slot) => (RaceMask.Value & (1 << slot)) != 0;

        public int Count(SlotKind kind)
        {
            int n = 0;
            foreach (var s in Slots) if (s.Kind == kind) n++;
            return n;
        }

        /// <summary>Whether the colour is free (or already this slot's).</summary>
        public bool ColorFree(int color, int forSlot)
        {
            for (int i = 0; i < Slots.Count; i++)
                if (i != forSlot && Slots[i].Kind != SlotKind.Empty && Slots[i].Color == color) return false;
            return true;
        }

        /// <summary>Every guest has tapped READY (the host is always ready).</summary>
        public bool AllReady
        {
            get
            {
                foreach (var s in Slots)
                    if (s.Kind == SlotKind.Human && s.ClientId != NetworkManager.ServerClientId && !s.Ready) return false;
                return true;
            }
        }

        // ------------------------------------------------------------------ guests -> host

        [Rpc(SendTo.Server)]
        void HelloRpc(FixedString32Bytes name, byte color, RpcParams rpc = default)
        {
            ulong id = rpc.Receive.SenderClientId;
            int slot = SlotOf(id);
            if (slot < 0) slot = Seat(id, name.ToString(), color);
            else
            {
                var s = Slots[slot];
                s.Name = new FixedString32Bytes(OnlineSession.Clean(name.ToString()));
                s.Color = (byte)FreeColor(color, slot);
                Slots[slot] = s;
            }
        }

        /// <summary>Asks for a car colour (taken colours are refused).</summary>
        public void PickColor(int color)
        {
            OnlineSession.PreferredColor = color;
            PickColorRpc((byte)color);
        }

        [Rpc(SendTo.Server)]
        void PickColorRpc(byte color, RpcParams rpc = default)
        {
            int slot = SlotOf(rpc.Receive.SenderClientId);
            if (slot < 0 || color >= OnlineColors.All.Length || !ColorFree(color, slot)) return;
            var s = Slots[slot];
            s.Color = color;
            Slots[slot] = s;
        }

        public void SetReady(bool ready) => ReadyRpc(ready);

        [Rpc(SendTo.Server)]
        void ReadyRpc(bool ready, RpcParams rpc = default)
        {
            int slot = SlotOf(rpc.Receive.SenderClientId);
            if (slot < 0 || Phase.Value != RoomPhase.Lobby) return;
            var s = Slots[slot];
            s.Ready = ready;
            Slots[slot] = s;
        }

        public void Rename(string name)
        {
            OnlineSession.PlayerName = name;
            HelloRpc(OnlineSession.PlayerName, (byte)OnlineSession.PreferredColor);
        }

        /// <summary>This phone has the race scene ready (the host waits for everyone before the countdown).</summary>
        public void ReportLoaded() => LoadedRpc(RaceNumber.Value);

        [Rpc(SendTo.Server)]
        void LoadedRpc(int race, RpcParams rpc = default)
        {
            if (race != RaceNumber.Value || Phase.Value != RoomPhase.Loading) return;
            _loaded.Add(rpc.Receive.SenderClientId);
        }

        // ------------------------------------------------------------------ host only

        int Seat(ulong clientId, string name, int color)
        {
            int slot = -1;
            for (int i = 0; i < Slots.Count && slot < 0; i++) if (Slots[i].Kind == SlotKind.Empty) slot = i;
            if (slot < 0)
            {
                // Full of bots: a player always beats a bot to a seat.
                for (int i = Slots.Count - 1; i >= 0 && slot < 0; i--) if (Slots[i].Kind == SlotKind.Bot) slot = i;
                if (slot < 0) return -1;
                Slots[slot] = default;
            }
            Slots[slot] = new LobbySlot
            {
                Kind = SlotKind.Human,
                ClientId = clientId,
                Name = new FixedString32Bytes(OnlineSession.Clean(name)),
                Color = (byte)FreeColor(color, slot),
            };
            return slot;
        }

        int FreeColor(int wanted, int slot)
        {
            if (wanted >= 0 && wanted < OnlineColors.All.Length && ColorFree(wanted, slot)) return wanted;
            for (int c = 0; c < OnlineColors.All.Length; c++) if (ColorFree(c, slot)) return c;
            return 0;
        }

        void OnClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId) return;
            if (SlotOf(clientId) < 0) Seat(clientId, "PLAYER", 255);
        }

        void OnClientDisconnected(ulong clientId)
        {
            int slot = SlotOf(clientId);
            if (slot >= 0)
            {
                Slots[slot] = default;
                _latest[slot] = null;
            }
            _loaded.Remove(clientId);
        }

        public bool CanAddBot => IsServer && Phase.Value == RoomPhase.Lobby && Count(SlotKind.Empty) > 0;

        public void AddBot()
        {
            if (!CanAddBot) return;
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].Kind != SlotKind.Empty) continue;
                Slots[i] = new LobbySlot
                {
                    Kind = SlotKind.Bot,
                    Name = new FixedString32Bytes("BOT " + FreeBotName()),
                    Color = (byte)FreeColor(-1, i),
                    Ready = true,
                };
                return;
            }
        }

        /// <summary>Host: fills every free seat with a bot.</summary>
        public void FillWithBots()
        {
            while (CanAddBot) AddBot();
        }

        string FreeBotName()
        {
            foreach (var n in BotNames)
            {
                bool used = false;
                foreach (var s in Slots) if (s.Kind == SlotKind.Bot && s.Name.ToString() == "BOT " + n) used = true;
                if (!used) return n;
            }
            return BotNames[0];
        }

        public void RemoveBot(int slot)
        {
            if (!IsServer || Phase.Value != RoomPhase.Lobby || slot < 0 || slot >= Slots.Count || Slots[slot].Kind != SlotKind.Bot) return;
            Slots[slot] = default;
        }

        /// <summary>Host: a bot's colour, cycled to the next free one.</summary>
        public void CycleBotColor(int slot)
        {
            if (!IsServer || slot < 0 || slot >= Slots.Count || Slots[slot].Kind != SlotKind.Bot) return;
            var s = Slots[slot];
            for (int k = 1; k <= OnlineColors.All.Length; k++)
            {
                int c = (s.Color + k) % OnlineColors.All.Length;
                if (!ColorFree(c, slot)) continue;
                s.Color = (byte)c;
                Slots[slot] = s;
                return;
            }
        }

        /// <summary>Host: picks the race (any race of any cup).</summary>
        public void SelectRace(int index)
        {
            if (!IsServer || Phase.Value != RoomPhase.Lobby) return;
            int count = Races.Count;
            if (count == 0) return;
            RaceIndex.Value = (index % count + count) % count;
            PlayerPrefs.SetInt("Online.Race", RaceIndex.Value);
        }

        /// <summary>Host: everyone into the race.</summary>
        public void StartRace()
        {
            if (!IsServer || Phase.Value != RoomPhase.Lobby || Race == null) return;
            byte mask = 0;
            for (int i = 0; i < Slots.Count; i++) if (Slots[i].Kind != SlotKind.Empty) mask |= (byte)(1 << i);
            RaceMask.Value = mask;
            RaceNumber.Value++;
            GoTime.Value = 0;
            _loaded.Clear();
            for (int i = 0; i < _latest.Length; i++) _latest[i] = null;
            _loadingSince = Time.unscaledTime;
            Phase.Value = RoomPhase.Loading;
        }

        /// <summary>Host: the race is over, everyone back to the lobby.</summary>
        public void BackToLobby()
        {
            if (!IsServer || Phase.Value == RoomPhase.Lobby) return;
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].Kind != SlotKind.Human || !Slots[i].Ready) continue;
                var s = Slots[i];
                s.Ready = false; // ready again for the next race
                Slots[i] = s;
            }
            GoTime.Value = 0;
            Phase.Value = RoomPhase.Lobby;
        }

        void Update()
        {
            if (!IsSpawned || !IsServer) return;
            if (Phase.Value == RoomPhase.Loading && GoTime.Value == 0 && EveryoneLoaded())
                GoTime.Value = NetworkManager.ServerTime.Time + CountdownLead;
            if (Phase.Value == RoomPhase.Loading && GoTime.Value > 0 && NetworkManager.ServerTime.Time >= GoTime.Value - CountdownLead + 0.1)
                Phase.Value = RoomPhase.Racing;

            if (Phase.Value == RoomPhase.Lobby) return;
            _sendTimer -= Time.unscaledDeltaTime;
            if (_sendTimer > 0f) return;
            _sendTimer = SendInterval;
            var batch = new List<CarState>(SlotCount);
            foreach (var s in _latest) if (s.HasValue && s.Value.Race == RaceNumber.Value) batch.Add(s.Value);
            if (batch.Count > 0 && NetworkManager.ConnectedClientsIds.Count > 1) StatesRpc(batch.ToArray());
        }

        bool EveryoneLoaded()
        {
            if (Time.unscaledTime - _loadingSince > LoadTimeout) return true; // a slow phone joins in late
            for (int i = 0; i < Slots.Count; i++)
                if (InRace(i) && Slots[i].Kind == SlotKind.Human && !_loaded.Contains(Slots[i].ClientId)) return false;
            return true;
        }

        // ------------------------------------------------------------------ car states

        /// <summary>Sends this phone's car (or, on the host, a bot) to the others.</summary>
        public void Submit(CarState state)
        {
            state.Race = RaceNumber.Value;
            state.Stamp = NetworkManager.ServerTime.Time;
            if (IsServer) _latest[state.Slot] = state;
            else SubmitRpc(state);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        void SubmitRpc(CarState state, RpcParams rpc = default)
        {
            if (state.Slot >= SlotCount || SlotOf(rpc.Receive.SenderClientId) != state.Slot || state.Race != RaceNumber.Value) return;
            if (_latest[state.Slot] is { } last && last.Stamp > state.Stamp) return; // late packet
            _latest[state.Slot] = state;
            StateReceived?.Invoke(state);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        void StatesRpc(CarState[] states)
        {
            foreach (var s in states)
                if (s.Race == RaceNumber.Value) StateReceived?.Invoke(s);
        }

        // ------------------------------------------------------------------ phase changes

        void OnPhaseChanged(RoomPhase previous, RoomPhase phase)
        {
            Changed?.Invoke();
            int me = LocalSlot;
            if (phase == RoomPhase.Loading && me >= 0 && InRace(me))
                GameSession.StartOnlineRace(Race);
            else if (phase == RoomPhase.Lobby && previous != RoomPhase.Lobby && GameSession.InRaceScene)
                GameSession.ReturnToMap();
        }
    }
}
