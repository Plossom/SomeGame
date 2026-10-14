using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace SomeGame.Online
{
    /// <summary>
    /// The connection to an online room. CREATE opens a private session (Unity Lobby + Relay) and hosts it;
    /// JOIN enters one by its six-letter code. The host's phone runs the room (<see cref="OnlineRoom"/>):
    /// slots, colours, bots, the race choice and the race start. Lives across scenes.
    /// For tests on one computer, the command line "-onlineLocal" skips the Unity services and connects
    /// straight over 127.0.0.1 (the room code is then LOCAL).
    /// </summary>
    public class OnlineSession : MonoBehaviour
    {
        public const int MaxPlayers = 8;
        const string NameKey = "Online.Name";
        const string ColorKey = "Online.Color";
        const string RoomPrefabPath = "Online/OnlineRoom";

        public enum Status { Offline, Connecting, InRoom }

        static OnlineSession _instance;
        public static OnlineSession Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("OnlineSession");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<OnlineSession>();
                return _instance;
            }
        }

        /// <summary>True while connected to a room (as host or guest).</summary>
        public static bool Active => _instance != null && _instance.State == Status.InRoom && OnlineRoom.Current != null;

        public Status State { get; private set; } = Status.Offline;
        public string Code { get; private set; } = "";
        public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        /// <summary>Something went wrong or the room closed: a short message for the player.</summary>
        public event Action<string> Failed;
        public event Action StateChanged;

        ISession _session;
        NetworkManager _network;
        bool _quitting; // shutting down on purpose: no "the room closed" message

        public static string PlayerName
        {
            get
            {
                string name = PlayerPrefs.GetString(NameKey, "");
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = RandomName();
                    PlayerName = name;
                }
                return name;
            }
            set
            {
                PlayerPrefs.SetString(NameKey, Clean(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>The colour last picked (asked for again when entering a room).</summary>
        public static int PreferredColor
        {
            get => PlayerPrefs.GetInt(ColorKey, 0);
            set { PlayerPrefs.SetInt(ColorKey, value); PlayerPrefs.Save(); }
        }

        public static string Clean(string name)
        {
            name = (name ?? "").Trim().ToUpperInvariant();
            if (name.Length > 12) name = name.Substring(0, 12);
            return name.Length == 0 ? "PLAYER" : name;
        }

        static readonly string[] First = { "TURBO", "DRIFT", "ZIPPY", "NITRO", "SNOWY", "LUCKY", "ROCKET", "PIXEL", "SPEEDY", "FUNKY", "MIGHTY", "SUNNY" };
        static readonly string[] Second = { "FOX", "OTTER", "PANDA", "TIGER", "MOOSE", "GECKO", "BADGER", "LYNX", "YAK", "HAWK" };

        public static string RandomName() =>
            Clean(First[UnityEngine.Random.Range(0, First.Length)] + " " + Second[UnityEngine.Random.Range(0, Second.Length)]);

        static bool LocalTest
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_STANDALONE
                foreach (var arg in Environment.GetCommandLineArgs())
                    if (arg == "-onlineLocal") return true;
                return ForceLocal;
#else
                return false;
#endif
            }
        }

        /// <summary>Development: connect over 127.0.0.1 without the Unity services (tests in the Editor).</summary>
        public static bool ForceLocal;

        // ------------------------------------------------------------------ create / join / leave

        /// <summary>How far opening or joining a room has got (0..1), and what it is doing.</summary>
        public float Progress { get; private set; }
        public string Step { get; private set; } = "";

        Task _leaving;
        int _attempt; // a cancelled or timed-out attempt finds a newer number and gives up

        const float StepTimeout = 20f;

        void Report(float progress, string step)
        {
            Progress = progress;
            Step = step;
        }

        public async void Create()
        {
            if (State != Status.Offline) return;
            int attempt = ++_attempt;
            SetState(Status.Connecting);
            try
            {
                Report(0.05f, "GETTING READY");
                await FinishLeaving();
                EnsureNetworkManager();
                if (LocalTest)
                {
                    Transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
                    if (!_network.StartHost()) throw new Exception("Could not start the local host.");
                    Code = "LOCAL";
                }
                else
                {
                    Report(0.15f, "CONNECTING TO THE SERVER");
                    await Limit(SignIn(), attempt);
                    Report(0.45f, "OPENING A ROOM");
                    var options = new SessionOptions { MaxPlayers = MaxPlayers, IsPrivate = true, Name = PlayerName + "'S ROOM" }.WithRelayNetwork();
                    var session = await Limit(MultiplayerService.Instance.CreateSessionAsync(options), attempt);
                    if (attempt != _attempt) { _ = session.LeaveAsync(); return; } // cancelled meanwhile
                    _session = session;
                    Code = _session.Code;
                    Watch(_session);
                }
                Report(0.9f, "SETTING UP THE ROOM");
                SpawnRoom();
                Report(1f, "READY");
                SetState(Status.InRoom);
            }
            catch (Exception e)
            {
                if (attempt == _attempt) Fail(Explain(e, "Could not create a room."));
            }
        }

        public async void Join(string code)
        {
            if (State != Status.Offline) return;
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length == 0) { Failed?.Invoke("ENTER THE ROOM CODE"); return; }
            int attempt = ++_attempt;
            SetState(Status.Connecting);
            try
            {
                Report(0.05f, "GETTING READY");
                await FinishLeaving();
                EnsureNetworkManager();
                if (LocalTest || code == "LOCAL")
                {
                    Transport.SetConnectionData("127.0.0.1", 7777);
                    if (!_network.StartClient()) throw new Exception("Could not start the local client.");
                }
                else
                {
                    Report(0.15f, "CONNECTING TO THE SERVER");
                    await Limit(SignIn(), attempt);
                    Report(0.45f, "LOOKING FOR " + code);
                    var session = await Limit(MultiplayerService.Instance.JoinSessionByCodeAsync(code), attempt);
                    if (attempt != _attempt) { _ = session.LeaveAsync(); return; }
                    _session = session;
                    Watch(_session);
                }
                Code = code;
                // The room object arrives from the host shortly; give up if it does not.
                Report(0.75f, "ENTERING THE ROOM");
                float until = Time.realtimeSinceStartup + 15f;
                while (OnlineRoom.Current == null && Time.realtimeSinceStartup < until && attempt == _attempt)
                    await Task.Yield();
                if (attempt != _attempt) return;
                if (OnlineRoom.Current == null) throw new Exception("No answer from the room.");
                Report(1f, "READY");
                SetState(Status.InRoom);
            }
            catch (Exception e)
            {
                if (attempt == _attempt) Fail(Explain(e, "Could not join that room."));
            }
        }

        /// <summary>Gives up on an attempt that is still connecting (the X on the card).</summary>
        public void Cancel()
        {
            if (State != Status.Connecting) return;
            _attempt++;
            Leave();
        }

        /// <summary>
        /// Leaves the room (the host closes it for everyone). You are offline at once; telling the server
        /// finishes in the background (a new room waits for that first).
        /// </summary>
        public void Leave()
        {
            _attempt++;
            var session = _session;
            _session = null;
            _quitting = true;
            if (_network != null && (_network.IsListening || _network.IsClient || _network.ShutdownInProgress)) _network.Shutdown();
            Code = "";
            Report(0f, "");
            if (session != null) _leaving = LeaveQuietly(session);
            _quitting = false;
            SetState(Status.Offline);
        }

        static async Task LeaveQuietly(ISession session)
        {
            try
            {
                var leave = session.LeaveAsync();
                await Task.WhenAny(leave, Task.Delay(8000)); // never wait for ever
            }
            catch (Exception e) { Debug.LogWarning("Leaving the session: " + e.Message); }
        }

        async Task FinishLeaving()
        {
            if (_leaving != null && !_leaving.IsCompleted) await _leaving;
            _leaving = null;
            // The network shuts down at the end of a frame: wait until it really has.
            for (int i = 0; i < 120 && _network != null && (_network.ShutdownInProgress || _network.IsListening); i++)
                await Task.Yield();
        }

        // A network step that takes too long (no signal, a stuck server) fails instead of hanging.
        async Task Limit(Task task, int attempt)
        {
            var done = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(StepTimeout)));
            if (attempt != _attempt) throw new OperationCanceledException();
            if (done != task) throw new TimeoutException();
            await task; // rethrows its error
        }

        async Task<T> Limit<T>(Task<T> task, int attempt)
        {
            await Limit((Task)task, attempt);
            return task.Result;
        }

        // ------------------------------------------------------------------ internals

        static async Task SignIn()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        UnityTransport Transport => _network.GetComponent<UnityTransport>();

        void EnsureNetworkManager()
        {
            if (_network != null) return;
            _network = NetworkManager.Singleton;
            if (_network == null)
            {
                var go = new GameObject("NetworkManager");
                DontDestroyOnLoad(go);
                var transport = go.AddComponent<UnityTransport>();
                transport.DisconnectTimeoutMS = 10000; // a phone that quit or lost signal frees its seat after 10 s
                _network = go.AddComponent<NetworkManager>();
                _network.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = transport,
                    EnableSceneManagement = false, // each phone loads the race itself (with its loading screen)
                    ConnectionApproval = false,
                    TickRate = 30,
                };
                var prefab = Resources.Load<GameObject>(RoomPrefabPath);
                if (prefab == null) throw new Exception("The room prefab is missing.");
                _network.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
            }
            _network.OnClientStopped -= OnClientStopped;
            _network.OnClientStopped += OnClientStopped;
            _network.OnClientDisconnectCallback -= OnDisconnect;
            _network.OnClientDisconnectCallback += OnDisconnect;
        }

        void SpawnRoom()
        {
            var prefab = Resources.Load<GameObject>(RoomPrefabPath);
            var room = Instantiate(prefab);
            room.GetComponent<NetworkObject>().Spawn(destroyWithScene: false);
        }

        void Watch(ISession session)
        {
            session.RemovedFromSession += () => { if (!_quitting && _session == session && State != Status.Offline) Fail("THE ROOM WAS CLOSED"); };
            session.Deleted += () => { if (!_quitting && _session == session && State != Status.Offline) Fail("THE ROOM WAS CLOSED"); };
        }

        // The local client stopped: the host left, the connection dropped, or we left on purpose.
        void OnClientStopped(bool wasHost)
        {
            if (_quitting || State == Status.Offline) return;
            Fail(wasHost ? "THE ROOM WAS CLOSED" : "THE HOST LEFT THE ROOM");
        }

        void OnDisconnect(ulong clientId)
        {
            if (_network == null || _network.IsServer || _quitting || State == Status.Offline) return;
            if (clientId == _network.LocalClientId || clientId == NetworkManager.ServerClientId)
                Fail("THE HOST LEFT THE ROOM");
        }

        void Fail(string message)
        {
            Debug.LogWarning("Online: " + message);
            Leave();
            Failed?.Invoke(message);
        }

        void SetState(Status state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke();
        }

        static string Explain(Exception e, string fallback)
        {
            Debug.LogWarning("Online: " + e);
            if (e is SessionException s)
            {
                switch (s.Error)
                {
                    case SessionError.SessionNotFound:
                    case SessionError.InvalidSessionIdentifier:
                    case SessionError.InvalidParameter:
                        return "NO ROOM WITH THAT CODE";
                    case SessionError.Forbidden:
                    case SessionError.InvalidOperation:
                        return "THAT ROOM IS FULL OR CLOSED";
                    case SessionError.RateLimitExceeded:
                        return "TOO MANY TRIES - WAIT A MOMENT";
                }
            }
            if (e is RequestFailedException || e is AuthenticationException) return "NO CONNECTION TO THE GAME SERVER";
            if (e is ServicesInitializationException) return "ONLINE PLAY IS NOT SET UP YET";
            if (e is TimeoutException) return "NO ANSWER - CHECK YOUR INTERNET";
            return fallback.ToUpperInvariant();
        }
    }
}
