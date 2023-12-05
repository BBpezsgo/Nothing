using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Unity.Netcode;

using Game.Components;
using Game.Managers;

using Networking;
using UI;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Netcode.Transports.WebSocket;
using Netcode.Transports.Offline;
using System.Net;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utilities
{
    public readonly struct LayerMaskNames
    {
        public const string Default = "Default";
        public const string Ground = "Ground";
        public const string Projectile = "Projectile";
        public const string PhotographyStudio = "PhotographyStudio";
        public const string Water = "Water";
    }

    public readonly struct DefaultLayerMasks
    {
        public static int JustDefault => LayerMask.GetMask(LayerMaskNames.Default);
        public static int JustGround => LayerMask.GetMask(LayerMaskNames.Ground);
        public static int PhotographyStudio => LayerMask.GetMask(LayerMaskNames.PhotographyStudio);

        /// <summary>
        /// <see cref="LayerMaskNames.Default"/> ; <see cref="LayerMaskNames.Ground"/>
        /// </summary>
        public static int Solids => LayerMask.GetMask(LayerMaskNames.Default, LayerMaskNames.Ground);
        /// <summary>
        /// <see cref="LayerMaskNames.Default"/> ; <see cref="LayerMaskNames.Projectile"/>
        /// </summary>
        public static int PossiblyDamagables => LayerMask.GetMask(LayerMaskNames.Default, LayerMaskNames.Projectile);
    }

    public struct AI
    {
        public delegate float GetPriority<T>(T @object);

        public static void SortTargets<T>(T[] targets, GetPriority<T> getPriority) where T : UnityEngine.Object
        {
            (T, float)[] priorities = new (T, float)[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                float priority = 0f;

                if ((UnityEngine.Object)targets[i] != null)
                { priority = getPriority?.Invoke(targets[i]) ?? 0f; }

                priorities[i] = (targets[i], priority);
            }

            Array.Sort(priorities, (a, b) => Comparer.Default.Compare(a.Item2, b.Item2));

            for (int i = 0; i < targets.Length; i++)
            { targets[i] = priorities[i].Item1; }
        }

        public static void SortTargets<T>(IList<T> targets, GetPriority<T> getPriority) where T : UnityEngine.Object
        {
            (T, float)[] priorities = new (T, float)[targets.Count];

            for (int i = 0; i < targets.Count; i++)
            {
                float priority = 0f;

                if ((UnityEngine.Object)targets[i] != null)
                { priority = getPriority?.Invoke(targets[i]) ?? 0f; }

                priorities[i] = (targets[i], priority);
            }

            Array.Sort(priorities, (a, b) => Comparer.Default.Compare(a.Item2, b.Item2));

            for (int i = 0; i < priorities.Length; i++)
            { targets[i] = priorities[i].Item1; }
        }

        public static void SortTargets(BaseObject[] targets, Vector3 origin, string team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.Team, team));

        public static void SortTargets(IList<BaseObject> targets, Vector3 origin, string team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.Team, team));

        public static void SortTargets(BaseObject[] targets, Vector3 origin, int team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.TeamHash, team));

        public static void SortTargets(IList<BaseObject> targets, Vector3 origin, int team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.TeamHash, team));
    }
}

namespace Game
{
    [Serializable]
    public struct CursorConfig
    {
        public Texture2D Texture;
        public Vector2 Hotspot;

        public readonly void Set() => CursorManager.SetCursor(Texture, Hotspot);
    }

    public static class ObjectGroups
    {
        static Transform game;
        static Transform effects;
        static Transform projectiles;
        static Transform items;

        public static Transform Game
        {
            get
            {
                if (game == null)
                {
                    GameObject obj = GameObject.Find("Game");
                    if (obj == null)
                    { obj = new GameObject("Game"); }
                    game = obj.transform;
                }
                return game;
            }
        }

        public static Transform Effects
        {
            get
            {
                GameObject obj = GameObject.Find("Effects");
                if (obj == null)
                { obj = new GameObject("Effects"); }
                effects = obj.transform;
                return effects;
            }
        }

        public static Transform Projectiles
        {
            get
            {
                GameObject obj = GameObject.Find("Projectiles");
                if (obj == null)
                { obj = new GameObject("Projectiles"); }
                projectiles = obj.transform;
                return projectiles;
            }
        }

        public static Transform Items
        {
            get
            {
                GameObject obj = GameObject.Find("Items");
                if (obj == null)
                { obj = new GameObject("Items"); }
                items = obj.transform;
                return items;
            }
        }
    }

    internal static class TheTerrain
    {
        static Terrain terrain;
        static float terrainBaseHeight = 0f;
        static bool hasTerrain = false;

        static void FindTerrainIfNone()
        {
            if (!hasTerrain)
            {
                terrain = GameObject.FindObjectOfType<Terrain>();
                hasTerrain = terrain != null;

                if (hasTerrain)
                { terrainBaseHeight = terrain.transform.position.y; }
            }
            else
            {
                hasTerrain = terrain != null;
            }
        }

        internal static Terrain Terrain
        {
            get
            {
                FindTerrainIfNone();

                return terrain;
            }
        }

        internal static float Height(Vector3 position)
        {
            FindTerrainIfNone();

            if (hasTerrain)
            { return terrain.SampleHeight(position) + terrainBaseHeight; }
            else
            { return 0f; }
        }
    }
}

public readonly partial struct ProfilerMarkers
{
    public static readonly Unity.Profiling.ProfilerMarker Animations = new("Utilities.Animations");
    public static readonly Unity.Profiling.ProfilerMarker UnitsBehavior = new("Game.Units.Behavior");
    public static readonly Unity.Profiling.ProfilerMarker VehicleEngine_Wheels = new("Game.VehicleEngine.Wheels");
    public static readonly Unity.Profiling.ProfilerMarker VehicleEngine_Basic = new("Game.VehicleEngine.Basic");
}

public static class NetcodeUtils
{
    public static bool IsServer => !IsOffline && NetworkManager.Singleton.IsServer;

    public static bool IsOfflineOrServer => IsOffline || NetworkManager.Singleton.IsServer;

    public static bool IsActiveOfflineOrServer => IsActiveOffline || NetworkManager.Singleton.IsServer;

    /// <summary>
    /// Returns <see langword="true"/> if there is <b>no <see cref="NetworkManager.Singleton"/></b>, if it is running in <b>offline mode</b>, or if the <see cref="NetworkManager"/> is <b>not listening</b>.
    /// </summary>
    public static bool IsOffline => IsActiveOffline || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

    public static bool IsActiveOffline => OfflineManager.IsActiveOffline;

    internal static bool IsClient => !IsOffline && NetworkManager.Singleton.IsClient;

    public static bool FindNetworkObject(ulong id, out NetworkObject networkObject)
    {
        if (NetcodeUtils.IsOffline)
        {
            networkObject = null;
            return false;
        }
        return NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out networkObject);
    }

    public static bool FindGameObject(ulong id, out GameObject @object)
    {
        if (!NetcodeUtils.FindNetworkObject(id, out NetworkObject networkObject))
        {
            @object = null;
            return false;
        }
        @object = networkObject.gameObject;
        return true;
    }

    /// <returns>
    ///   Current NetworkConfig as a string depending on the current transport as follows:
    ///   <list type="table">
    ///     <item>
    ///       <term> <see cref="UnityTransport"/> </term>
    ///       <description> Socket </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="WebSocketTransport"/> </term>
    ///       <description> URL </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="OfflineTransport"/> </term>
    ///       <description> "offline" </description>
    ///     </item>
    ///   </list>
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public static string NetworkConfig
    {
        get
        {
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
            { return $"{unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}"; }

            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is WebSocketTransport webSocketTransport)
            { return $"{(webSocketTransport.SecureConnection ? "wss" : "ws")}://{webSocketTransport.ConnectAddress}:{webSocketTransport.Port}{webSocketTransport.Path}"; }

            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is OfflineTransport)
            { return "offline"; }

            throw new NotImplementedException($"Unknown netcode transport {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType()}");
        }
    }

    static IEnumerator SetConnectionData(string input, Action<string> callback, UnityEngine.Object context = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            callback?.Invoke("Input is empty");
            yield break;
        }

        input = input.Trim();

        if (!input.Contains("://"))
        {
            input = "udp://" + input;
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out Uri uri))
        {
            callback?.Invoke("Invalid URI");
            yield break;
        }

        switch (uri.Scheme)
        {
            case "udp":
                yield return SetUDPConnectionData(uri, callback, context);
                yield break;

            case "ws":
            case "wss":
                yield return SetWebSocketConnectionData(uri, callback, context);
                yield break;

            default:
                callback?.Invoke($"Unknown scheme \"{uri.Scheme}\"");
                yield break;
        }
    }

    static IEnumerator SetUDPConnectionData(Uri uri, Action<string> callback, UnityEngine.Object context = null)
    {
        if (!NetworkManager.Singleton.gameObject.TryGetComponent(out UnityTransport unityTransport))
        {
            callback?.Invoke($"UDP not supported :(");
            yield break;
        }

        string socketAddress = null;

        if (uri.IsDefaultPort)
        {
            callback?.Invoke($"No port specified");
            yield break;
        }

        if (uri.Port < 1 || uri.Port > ushort.MaxValue)
        {
            callback?.Invoke($"Invalid port {uri.Port}");
            yield break;
        }

        if (!IPAddress.TryParse(uri.Host ?? "", out IPAddress address))
        {
            Debug.Log($"Resolving hostname \"{uri.Host}\" ...", context);
            Task<IPHostEntry> dnsTask = Dns.GetHostEntryAsync(uri.Host);

            yield return new WaitUntil(() => dnsTask.IsCompleted);

            if (!dnsTask.IsCompletedSuccessfully || dnsTask.Result == null)
            {
                Debug.Log($"[{nameof(NetcodeUtils)}]: Failed to resolve \"{uri.Host}\"", context);

                callback?.Invoke($"Failed to resolve \"{uri.Host}\"");
                yield break;
            }

            IPHostEntry dnsResult = dnsTask.Result;

            if (dnsResult.AddressList.Length == 0)
            {
                Debug.Log($"[{nameof(NetcodeUtils)}]: DNS entry \"{uri.Host}\" does not have any address", context);

                callback?.Invoke($"DNS entry \"{uri.Host}\" does not have any address");
                yield break;
            }

            Debug.Log($"Hostname (\"{uri.Host}\") result: {dnsResult.AddressList.ToReadableString()}", context);

            socketAddress = dnsResult.AddressList[0].ToString();
        }
        else
        {
            socketAddress = address.ToString();

            if (socketAddress != uri.Host)
            {
                callback?.Invoke($"Invalid IP Address \"{uri.Host}\"");
                yield break;
            }
        }

        NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
        Debug.Log($"[{nameof(NetcodeUtils)}]: {nameof(NetworkManager.Singleton.NetworkConfig.NetworkTransport)} set to {nameof(UnityTransport)}", context);

        unityTransport.SetConnectionData(socketAddress, (ushort)uri.Port, socketAddress);
        Debug.Log($"[{nameof(NetcodeUtils)}]: Connection data set to {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}", context);
        callback?.Invoke(null);
        yield break;
    }

    static IEnumerator SetWebSocketConnectionData(Uri uri, Action<string> callback, UnityEngine.Object context = null)
    {
        if (!NetworkManager.Singleton.gameObject.TryGetComponent(out WebSocketTransport webSocketTransport))
        {
            callback?.Invoke($"WebSocket not supported :(");
            yield break;
        }

        if (uri.IsDefaultPort)
        {
            callback?.Invoke($"No port specified");
            yield break;
        }

        if (uri.Port < 1 || uri.Port > ushort.MaxValue)
        {
            callback?.Invoke($"Invalid port {uri.Port}");
            yield break;
        }

        NetworkManager.Singleton.NetworkConfig.NetworkTransport = webSocketTransport;
        Debug.Log($"[{nameof(NetcodeUtils)}]: {nameof(NetworkManager.Singleton.NetworkConfig.NetworkTransport)} set to {nameof(WebSocketTransport)}", context);

        webSocketTransport.AllowForwardedRequest = false;
        webSocketTransport.CertificateBase64String = "";
        webSocketTransport.ConnectAddress = uri.Host;
        webSocketTransport.Port = (ushort)uri.Port;
        webSocketTransport.SecureConnection = (uri.Scheme == "wss");
        webSocketTransport.Path = uri.AbsolutePath;

        Debug.Log($"[{nameof(NetcodeUtils)}]: Connection data set to {(webSocketTransport.SecureConnection ? "wss" : "ws")}://{webSocketTransport.ConnectAddress}:{webSocketTransport.Port}{webSocketTransport.Path}", context);
        callback?.Invoke(null);
        yield break;
    }

    public static IEnumerator HostAsync(string input, Action<string> callback, UnityEngine.Object context = null)
    {
        string socketComputeError = null;
        yield return SetConnectionData(input, result => { socketComputeError = result; });

        if (socketComputeError != null)
        {
            callback?.Invoke(socketComputeError);
            yield break;
        }

        Debug.Log($"[{nameof(NetcodeUtils)}]: Start server on {NetworkConfig} ...", context);

        bool success = NetworkManager.Singleton.StartServer();

        if (success)
        {
            Debug.Log($"[{nameof(NetcodeUtils)}]: Server started on {NetworkConfig}", context);
        }
        else
        {
            callback?.Invoke($"Failed to start server on {NetworkConfig}");
            Debug.LogError($"[{nameof(NetcodeUtils)}]: Failed to start server on {NetworkConfig}", context);
        }
    }

    public static IEnumerator ConnectAsync(string input, Action<string> callback, UnityEngine.Object context = null)
    {
        string socketComputeError = null;
        yield return SetConnectionData(input, result => { socketComputeError = result; });

        if (socketComputeError != null)
        {
            callback?.Invoke(socketComputeError);
            yield break;
        }

        Debug.Log($"[{nameof(NetcodeUtils)}]: Start client on {NetworkConfig} ...", context);

        bool success = NetworkManager.Singleton.StartClient();

        if (success)
        {
            Debug.Log($"[{nameof(NetcodeUtils)}]: Client started on {NetworkConfig}", context);
        }
        else
        {
            callback?.Invoke($"Failed to start client on {NetworkConfig}");
            Debug.LogError($"[{nameof(NetcodeUtils)}]: Failed to start client on {NetworkConfig}", context);
        }
    }
}

namespace InputUtils
{
    public delegate bool InputConditionEnabler();

    public delegate void SimpleInputEvent<T>(T sender);

    public class AdvancedInput : IComparable<AdvancedInput>
    {
        public static float ScreenSize => Maths.Sqrt((Screen.width * Screen.width) + (Screen.height * Screen.height));

        protected readonly InputConditionEnabler ConditionEnabler;
        public readonly int Priority;
        protected readonly Type OwnedBy;

        public virtual bool Enabled => ConditionEnabler?.Invoke() ?? true;

        public AdvancedInput(int priority)
            : this(priority, null) { }

        public AdvancedInput(int priority, InputConditionEnabler conditionEnabler)
        {
            this.Priority = priority;
            this.ConditionEnabler = conditionEnabler;

            System.Diagnostics.StackTrace stack = new(false);
            for (int i = 0; i < stack.FrameCount; i++)
            {
                System.Diagnostics.StackFrame frame = stack.GetFrame(i);
                System.Reflection.MethodBase method = frame.GetMethod();
                if (method.IsConstructor)
                { continue; }
                Type declaringType = method.DeclaringType;
                OwnedBy = declaringType;
                break;
            }
        }

        public int CompareTo(AdvancedInput other)
            => Comparer.DefaultInvariant.Compare(other.Priority, this.Priority);
    }

    [Serializable]
    public class AdvancedMouse : AdvancedInput
    {
        public delegate void DragEvent(Vector2 start, Vector2 current);
        public delegate void DraggedEvent(Vector2 start, Vector2 end);

        public event DragEvent OnDrag;
        public event DraggedEvent OnDragged;
        public event SimpleInputEvent<AdvancedMouse> OnClick;
        public event SimpleInputEvent<AdvancedMouse> OnDown;

        public readonly int ButtonID;

        public static Vector2 Position => Input.mousePosition;

        [SerializeField, ReadOnly] bool ClickedOnUI;

        public Vector2 DragStart { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDragging => Drag && !ClickedOnUI;
        [SerializeField, ReadOnly] bool Drag;
        public const float DragThreshold = 25f;
        public static readonly float DragThresholdSqr = Maths.Sqrt(DragThreshold);

        float PressedAt;
        readonly float UpTimeout;

        [SerializeField, ReadOnly] bool DownInvoked;
        [SerializeField, ReadOnly] bool UpInvoked;

        public float HoldTime => Time.unscaledTime - PressedAt;

        public AdvancedMouse(int buttonId, int priority)
            : this(buttonId, priority, null, 0f) { }

        public AdvancedMouse(int buttonId, int priority, InputConditionEnabler conditionEnabler)
            : this(buttonId, priority, conditionEnabler, 0f) { }

        public AdvancedMouse(int buttonId, int priority, float upTimeout)
            : this(buttonId, priority, null, upTimeout) { }

        public AdvancedMouse(int buttonId, int priority, InputConditionEnabler conditionEnabler, float upTimeout)
            : base(priority, conditionEnabler)
        {
            this.ButtonID = buttonId;
            this.UpTimeout = upTimeout;
            MouseManager.RegisterInput(this);
        }

        public void Update()
        {
            if (!Enabled)
            {
                Reset();
                return;
            }

            this.IsActive = true;

            if (Input.GetMouseButtonDown(ButtonID))
            { Down(); }
            else if (Input.GetMouseButtonUp(ButtonID))
            { Up(); }
            else if (Input.GetMouseButton(ButtonID))
            { Hold(); }
        }

        public void Reset()
        {
            this.DragStart = Vector2.zero;
            this.ClickedOnUI = false;
            this.Drag = false;
            this.UpInvoked = true;
            this.DownInvoked = false;
            this.PressedAt = 0f;
            this.IsActive = false;
        }

        void Down()
        {
            DownInvoked = true;
            DragStart = Position;
            Drag = false;
            PressedAt = Time.unscaledTime;
            UpInvoked = false;
            ClickedOnUI = MouseManager.IsOverUI(Position);

            if (!ClickedOnUI)
            {
                try
                { OnDown?.Invoke(this); }
                catch (Exception exception)
                { Debug.LogException(exception); }
            }
        }

        void Hold()
        {
            if (!DownInvoked)
            { return; }

            if (!Drag && (Position - DragStart).sqrMagnitude > DragThresholdSqr)
            { Drag = true; }

            if (Drag)
            {
                if (!ClickedOnUI)
                {
                    try
                    { OnDrag?.Invoke(DragStart, Position); }
                    catch (Exception exception)
                    { Debug.LogException(exception); }
                }
            }

            if (UpTimeout != 0f && UpTimeout < HoldTime)
            { Up(); }
        }

        void Up()
        {
            if (!DownInvoked)
            { return; }

            if (!UpInvoked)
            {
                if (Drag)
                {
                    if (!ClickedOnUI)
                    {
                        try
                        { OnDragged?.Invoke(DragStart, Position); }
                        catch (Exception exception)
                        { Debug.LogException(exception); }
                    }
                }
                else
                {
                    if (!ClickedOnUI)
                    {
                        try
                        { OnClick?.Invoke(this); }
                        catch (Exception exception)
                        { Debug.LogException(exception); }
                    }
                }
            }

            DragStart = Vector2.zero;
            Drag = false;
            UpInvoked = true;
            PressedAt = 0f;
            IsActive = false;
        }

        public void DebugDraw()
        {
            Vector2 outerPointV = Vector2.up * 10;
            Vector2 outerPointH = Vector2.left * 10;

            Vector2 position = AdvancedMouse.Position;
            position = GUIUtils.TransformPoint(position);

            Color color = Color.white;

            if (ClickedOnUI)
            { color = Color.red; }

            position += new Vector2(1, -1);

            GLUtils.DrawLine(position - outerPointH, position + outerPointH, 1.5f, color);
            GLUtils.DrawLine(position - outerPointV, position + outerPointV, 1.5f, color);

            GUI.Label(new Rect(position - new Vector2(0, 20), new Vector2(200, 20)), $"{this.HoldTime:####.00} ms");
            GUI.Label(new Rect(position - new Vector2(0, 40), new Vector2(200, 20)), this.OwnedBy.Name);
        }
    }

    [Serializable]
    public class AdvancedTouch : AdvancedInput
    {
        public event SimpleInputEvent<AdvancedTouch> OnClick;
        public event SimpleInputEvent<AdvancedTouch> OnDown;
        public event SimpleInputEvent<AdvancedTouch> OnMove;
        public event SimpleInputEvent<AdvancedTouch> OnUp;
        public event SimpleInputEvent<AdvancedTouch> OnCancelled;

        [SerializeField, ReadOnly] Touch Touch;

        public TouchPhase Phase => Touch.phase;

        public Vector2 Position => Touch.position;

        public Vector2 PositionDelta => Touch.deltaPosition;

        [SerializeField, ReadOnly] public int FingerID;
        public bool IsActive => FingerID != -1;
        public bool IsActiveAndCaptured => IsCaptured && IsActive;

        [SerializeField, ReadOnly] bool ClickedOnUI;

        float PressedAt;
        readonly float UpTimeout;
        readonly RectInt ValidScreenRect;
        bool DownInvoked;
        bool UpInvoked;

        [ReadOnly] public bool IsCaptured;

        public bool IsHolding { get; private set; }

        public AdvancedTouch(int priority) : base(priority)
        {
            MouseManager.RegisterInput(this);
            ValidScreenRect = new RectInt(0, 0, 0, 0);
        }

        public AdvancedTouch(int priority, InputConditionEnabler conditionEnabler) : base(priority, conditionEnabler)
        {
            MouseManager.RegisterInput(this);
            ValidScreenRect = new RectInt(0, 0, 0, 0);
        }

        public AdvancedTouch(int priority, RectInt validScreenRect) : base(priority)
        {
            MouseManager.RegisterInput(this);
            ValidScreenRect = validScreenRect;
        }

        public AdvancedTouch(int priority, InputConditionEnabler conditionEnabler, RectInt validScreenRect) : base(priority, conditionEnabler)
        {
            MouseManager.RegisterInput(this);
            ValidScreenRect = validScreenRect;
        }

        public float HoldTime => Time.unscaledTime - PressedAt;

        public void Update()
        {
            // if (!Input.touchSupported) return;
            if (!Enabled)
            {
                FingerID = -1;
                Touch = default;
                IsCaptured = false;
                IsHolding = false;
                return;
            }

            Touch[] touches = Input.touches;

            if (FingerID != -1)
            {
                if (!MouseManager.IsTouchCaptured(this.FingerID, this))
                {
                    for (int i = 0; i < touches.Length; i++)
                    {
                        if (touches[i].fingerId == FingerID)
                        {
                            Touch = touches[i];
                            UpdateInternal();
                            return;
                        }
                    }
                }

                FingerID = -1;
                Touch = default;
                IsCaptured = false;
                IsHolding = false;
            }

            for (int i = 0; i < touches.Length; i++)
            {
                if (MouseManager.IsTouchCaptured(touches[i].fingerId, this))
                { continue; }

                Touch = touches[i];

                if (ValidScreenRect.size == Vector2Int.zero || ValidScreenRect.Contains(new Vector2Int(Maths.RoundToInt(Position.x), Maths.RoundToInt(Position.y))))
                {
                    FingerID = Touch.fingerId;
                    UpdateInternal();
                    return;
                }
            }

            FingerID = -1;
            Touch = default;
            IsCaptured = false;
            IsHolding = false;
        }

        void UpdateInternal()
        {
            switch (Touch.phase)
            {
                case TouchPhase.Began:
                    OnBegan();
                    break;
                case TouchPhase.Moved:
                    OnMoved();
                    break;
                case TouchPhase.Stationary:
                    OnStationary();
                    break;
                case TouchPhase.Ended:
                    OnEnded();
                    break;
                case TouchPhase.Canceled:
                    OnCanceled();
                    break;
                default:
                    break;
            }
        }

        void OnBegan()
        {
            DownInvoked = true;
            PressedAt = Time.unscaledTime;
            UpInvoked = false;
            ClickedOnUI = MouseManager.IsOverUI(Position);
            IsHolding = true;

            if (ClickedOnUI) return;

            OnDown?.Invoke(this);
        }

        void OnMoved()
        {
            IsHolding = false;

            if (ClickedOnUI) return;

            OnMove?.Invoke(this);
        }

        void OnStationary()
        {
            if (ClickedOnUI) return;

            if (UpTimeout != 0f && UpTimeout < HoldTime)
            {
                OnEnded();
                return;
            }
        }

        void OnEnded()
        {
            if (!DownInvoked) return;

            if (!UpInvoked)
            {
                if (!ClickedOnUI)
                {
                    OnClick?.Invoke(this);
                    OnUp?.Invoke(this);
                }
            }

            UpInvoked = true;
            PressedAt = 0f;
            FingerID = -1;
            IsCaptured = false;
            IsHolding = false;
        }

        void OnCanceled()
        {
            if (!ClickedOnUI)
            { OnCancelled?.Invoke(this); }

            UpInvoked = true;
            PressedAt = 0f;
            FingerID = -1;
            IsCaptured = false;
            IsHolding = false;
        }

        public void Reset()
        {
            ClickedOnUI = false;
            UpInvoked = true;
            DownInvoked = false;
            PressedAt = 0f;
            FingerID = -1;
            IsCaptured = false;
            IsHolding = false;
        }

        public void DebugDraw()
        {
            if (!IsActive) return;

            Vector2 position = this.Position;

            Color color;

            if (ClickedOnUI)
            {
                color = Color.red;
            }
            else
            {
                color = Phase switch
                {
                    TouchPhase.Began => Color.cyan,
                    TouchPhase.Moved => Color.blue,
                    TouchPhase.Stationary => Color.white,
                    TouchPhase.Ended => Color.yellow,
                    TouchPhase.Canceled => Color.magenta,
                    _ => Color.white,
                };
            }

            Vector2 guiPosition = GUIUtils.TransformPoint(position);

            float radius = 20;

            GLUtils.DrawCircle(guiPosition / 4.3f, radius, 2, color, 16);

            GUIStyle style = new(GUI.skin.label)
            {
                fontSize = 48,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                },
            };

            GUIStyle styleOutline = new(GUI.skin.label)
            {
                fontSize = 48,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState()
                {
                    textColor = Color.black,
                },
            };

            Vector2 textOffset = new(radius, -radius);
            textOffset *= 4.3f / 1.4f;

            int line = 1;

            void Label(string text)
            {
                Vector2 position = guiPosition + textOffset + new Vector2(0, style.fontSize * -line);
                Vector2 size = new(Screen.width, style.fontSize);
                GUI.enabled = false;
                GUI.Label(new Rect(position + (Vector2.one * 3), size), text, styleOutline);
                GUI.Label(new Rect(position, size), text, style);
                GUI.enabled = true;
                line++;
            }

            Label($"{this.HoldTime:####.00} ms");
            Label(this.OwnedBy.Name);

            if (IsCaptured)
            { Label("Captured"); }
            else
            { Label("Not Captured"); }

            if (IsHolding)
            { Label("Holding"); }
            else
            { Label("Moving"); }
        }
    }

    public class TouchZoom : AdvancedInput
    {
        public delegate void ZoomEvent(TouchZoom sender, float delta);

        public event ZoomEvent OnZoom;
        public event SimpleInputEvent<AdvancedTouch> OnMove;

        readonly AdvancedTouch Touch1;
        readonly AdvancedTouch Touch2;

        (Vector2 Start, Vector2 Current) Touch1Position;
        (Vector2 Start, Vector2 Current) Touch2Position;

        float StartDistance;
        float LastDistanceDiff;
        float Distance => Vector2.Distance(Touch1Position.Current, Touch2Position.Current);

        public bool BothTouchActive => Touch1.IsActive && Touch2.IsActive;
        public bool BothTouchActiveAndCaptured => Touch1.IsActiveAndCaptured && Touch2.IsActiveAndCaptured;

        public Vector2 PositionDelta
        {
            get
            {
                if (BothTouchActive) return Vector2.zero;
                if (Touch1.IsActive) return Touch1.PositionDelta;
                if (Touch2.IsActive) return Touch2.PositionDelta;
                return Vector2.zero;
            }
        }

        public TouchZoom(int priority, InputConditionEnabler condition) : base(priority, condition)
        {
            Touch1 = new AdvancedTouch(priority, condition);
            Touch2 = new AdvancedTouch(priority, condition);

            Touch1.OnDown += OnDown1;
            Touch2.OnDown += OnDown2;

            Touch1.OnMove += OnMove1;
            Touch2.OnMove += OnMove2;
        }

        public TouchZoom(int priority) : this(priority, null)
        { }

        void OnDown1(AdvancedTouch sender)
        {
            Touch1Position = (sender.Position, sender.Position);
            StartZooming();
        }

        void OnDown2(AdvancedTouch sender)
        {
            Touch2Position = (sender.Position, sender.Position);
            StartZooming();
        }

        void StartZooming()
        {
            StartDistance = Vector2.Distance(Touch1Position.Start, Touch2Position.Start);
            LastDistanceDiff = Distance - StartDistance;
        }

        void OnMove1(AdvancedTouch sender)
        {
            sender.IsCaptured = true;
            if (!Touch2.IsActive)
            {
                OnMove?.Invoke(sender);

                Touch1Position = (sender.Position, sender.Position);
                StartZooming();
                return;
            }

            Touch1Position.Current = sender.Position;
            UpdateInternal();
        }

        void OnMove2(AdvancedTouch sender)
        {
            sender.IsCaptured = true;
            if (!Touch1.IsActive)
            {
                OnMove?.Invoke(sender);

                Touch2Position = (sender.Position, sender.Position);
                StartZooming();
                return;
            }

            Touch2Position.Current = sender.Position;
            UpdateInternal();
        }

        void UpdateInternal()
        {
            if (!BothTouchActiveAndCaptured) return;

            if (StartDistance == 0)
            {
                StartDistance = Distance;
                return;
            }

            float distanceDiff = Distance - StartDistance;
            float distanceDelta = LastDistanceDiff - distanceDiff;

            OnZoom?.Invoke(this, distanceDelta / ScreenSize);

            LastDistanceDiff = distanceDiff;
        }
    }

    public class PriorityKey : AdvancedInput
    {
        public delegate void KeyEvent();

        public event KeyEvent OnDown;
        public event KeyEvent OnHold;
        public event KeyEvent OnUp;

        public readonly KeyCode Key;

        public PriorityKey(KeyCode key, int priority)
            : this(key, priority, null) { }

        public PriorityKey(KeyCode key, int priority, InputConditionEnabler conditionEnabler)
            : base(priority, conditionEnabler)
        {
            this.Key = key;
            KeyboardManager.Register(this);
        }

        public bool Update()
        {
            if (!Enabled)
            { return false; }

            bool consumed = false;

            if (OnDown != null && Input.GetKeyDown(Key))
            {
                OnDown.Invoke();
                consumed = true;
            }

            if (OnHold != null && Input.GetKey(Key))
            {
                OnHold.Invoke();
                consumed = true;
            }

            if (OnUp != null && Input.GetKeyUp(Key))
            {
                OnUp.Invoke();
                consumed = true;
            }

            return consumed;
        }
    }
}

public static partial class ListUtils
{
    public static string ToReadableString<T1, T2>(this NetworkList<T1> self, IReadOnlyDictionary<T1, T2> converter) where T1 : unmanaged, IEquatable<T1>
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        for (int i = 0; i < self.Count; i++)
        {
            if (i > 0)
            { builder.Append(", "); }
            T1 element = self[i];

            if (!converter.TryGetValue(element, out T2 converted))
            { converted = default; }

            string str;
            if (converted is UnityEngine.Object convertedO)
            { str = (convertedO == null) ? "null" : convertedO.ToString(); }
            else
            { str = (converted == null) ? "null" : converted.ToString(); }

            builder.Append(str);
        }

        builder.Append(" }");

        return builder.ToString();
    }
}

public static class IObjectExtensions
{
    public static ulong? GetNetworkID(this IComponent self)
    {
        if (!self.TryGetComponentInChildren(out NetworkObject networkObject))
        {
            Debug.LogError($"Object {self.GetGameObject()} does not have a {nameof(NetworkObject)} component", self.GetGameObject());
            return null;
        }
        return networkObject.NetworkObjectId;
    }

    public static ulong GetNetworkIDForce(this IComponent self)
    {
        if (!self.TryGetComponentInChildren(out NetworkObject networkObject))
        { throw new MissingComponentException($"Object {self.GetGameObject()} does not have a {nameof(NetworkObject)} component"); }

        return networkObject.NetworkObjectId;
    }

    public static bool TryGetNetworkID(this IComponent self, out ulong networkID)
    {
        if (!self.TryGetComponentInChildren(out NetworkObject networkObject))
        {
            networkID = default;
            return false;
        }
        networkID = networkObject.NetworkObjectId;
        return true;
    }
}
