using System.Collections.Generic;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networking.Managers
{
    public class NetcodeManager : SingleInstance<NetcodeManager>
    {
        bool BaseEventsRegistered;
        bool SceneEventsRegistered;

        internal struct SceneLoadInfo
        {
            internal readonly string SceneName;
            internal readonly LoadSceneMode LoadSceneMode;
            internal readonly AsyncOperation AsyncOperation;
            internal bool IsDone;
            internal bool IsTimedOut;

            public SceneLoadInfo(string sceneName, LoadSceneMode loadSceneMode)
                : this(sceneName, loadSceneMode, null)
            { }

            internal SceneLoadInfo(string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
            {
                this.SceneName = sceneName;
                this.LoadSceneMode = loadSceneMode;
                this.AsyncOperation = asyncOperation;
                this.IsDone = false;
                this.IsTimedOut = false;
            }
        }

        internal struct SceneUnloadInfo
        {
            internal readonly string SceneName;
            internal readonly AsyncOperation AsyncOperation;
            internal bool IsDone;
            internal bool IsTimedOut;

            internal SceneUnloadInfo(string sceneName)
                : this(sceneName, null)
            { }

            internal SceneUnloadInfo(string sceneName, AsyncOperation asyncOperation)
            {
                this.SceneName = sceneName;
                this.AsyncOperation = asyncOperation;
                this.IsDone = false;
                this.IsTimedOut = false;
            }
        }

        readonly Dictionary<ulong, SceneLoadInfo> sceneLoadings = new();
        readonly Dictionary<ulong, SceneUnloadInfo> sceneUnloadings = new();
        readonly List<ulong> sceneSynchronizations = new();

        internal IReadOnlyDictionary<ulong, SceneLoadInfo> SceneLoadings => sceneLoadings;
        internal IReadOnlyDictionary<ulong, SceneUnloadInfo> SceneUnloadings => sceneUnloadings;

        ImguiWindow Window;

        void Start()
        {
            NetcodeVariableSerializers.Init();

            Window = IMGUIManager.Instance.CreateWindow(new Rect(5f, 5f, 250f, 150f));
            Window.Title = "Netcode Scenes";
            Window.Visible = true;
            Window.DrawContent = OnWindowGUI;
        }

        void OnWindowGUI()
        {
            foreach (KeyValuePair<ulong, SceneLoadInfo> info in sceneLoadings)
            {
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(200f));
                GUILayout.Label(info.Key == NetworkManager.ServerClientId ? "Server" : $"Client #{info.Key}");

                if (info.Value.IsDone)
                {
                    if (info.Value.IsTimedOut)
                    {
                        Rect progressBarRect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, 18f, GUI.skin.GetStyle("progress-bar-bg"));

                        GUI.Box(progressBarRect, GUIContent.none, GUI.skin.GetStyle("progress-bar-bg"));

                        GUI.Label(progressBarRect, $"Timed out");
                    }
                    else
                    {
                        Rect progressBarRect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, 18f, GUI.skin.GetStyle("progress-bar-bg"));

                        GUI.Box(progressBarRect, GUIContent.none, GUI.skin.GetStyle("progress-bar-fg"));

                        GUI.Label(progressBarRect, $"\"{info.Value.SceneName}\" loaded");
                    }
                }
                else if (info.Value.AsyncOperation != null)
                {
                    Rect progressBarRect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, 18f, GUI.skin.GetStyle("progress-bar-bg"));

                    GUI.Box(progressBarRect, GUIContent.none, GUI.skin.GetStyle("progress-bar-bg"));

                    Rect progressBarFgRect = progressBarRect;
                    progressBarFgRect.width *= Maths.Clamp01(info.Value.AsyncOperation.progress);
                    GUI.Box(progressBarFgRect, GUIContent.none, GUI.skin.GetStyle("progress-bar-fg"));

                    GUI.Label(progressBarRect, $"Loading \"{info.Value.SceneName}\" {info.Value.AsyncOperation.progress:P}");
                }

                GUILayout.EndVertical();
            }

            foreach (KeyValuePair<ulong, SceneUnloadInfo> info in sceneUnloadings)
            {
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(200f));
                GUILayout.Label(info.Key == NetworkManager.ServerClientId ? "Server" : $"Client #{info.Key}");

                if (info.Value.IsDone)
                {
                    if (info.Value.IsTimedOut)
                    {
                        Rect progressBarRect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, 18f, GUI.skin.GetStyle("progress-bar-bg"));

                        GUI.Box(progressBarRect, GUIContent.none, GUI.skin.GetStyle("progress-bar-bg"));

                        GUI.Label(progressBarRect, $"Timed out");
                    }
                    else
                    {
                        Rect progressBarRect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, 18f, GUI.skin.GetStyle("progress-bar-bg"));

                        GUI.Box(progressBarRect, GUIContent.none, GUI.skin.GetStyle("progress-bar-fg"));

                        GUI.Label(progressBarRect, $"\"{info.Value.SceneName}\" unloaded");
                    }
                }
                else if (info.Value.AsyncOperation != null)
                {
                    Rect progressBarRect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, 18f, GUI.skin.GetStyle("progress-bar-bg"));

                    GUI.Box(progressBarRect, GUIContent.none, GUI.skin.GetStyle("progress-bar-bg"));

                    Rect progressBarFgRect = progressBarRect;
                    progressBarFgRect.width *= Maths.Clamp01(info.Value.AsyncOperation.progress);
                    GUI.Box(progressBarFgRect, GUIContent.none, GUI.skin.GetStyle("progress-bar-fg"));

                    GUI.Label(progressBarRect, $"Unloading \"{info.Value.SceneName}\" {info.Value.AsyncOperation.progress:P}");
                }

                GUILayout.EndVertical();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        void FixedUpdate()
        {
            Window.Visible = sceneLoadings.Count != 0 && sceneUnloadings.Count != 0;

            if (!BaseEventsRegistered && NetworkManager.Singleton != null)
            {
                BaseEventsRegistered = true;
                NetworkManager.Singleton.OnServerStopped += Singleton_OnServerStopped;
                NetworkManager.Singleton.OnServerStarted += Singleton_OnServerStarted;

                NetworkManager.Singleton.OnClientStopped += Singleton_OnClientStopped;
                NetworkManager.Singleton.OnClientStarted += Singleton_OnClientStarted;

                NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
                NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;

                // Debug.Log($"[{nameof(NetcodeManager)}]: Base events registered", this);
            }

            if (!SceneEventsRegistered && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                SceneEventsRegistered = true;
                NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
                NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadEventCompleted;
                NetworkManager.Singleton.SceneManager.OnSynchronize += OnSceneSynchronize;
                NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnSceneSynchronizeComplete;
                NetworkManager.Singleton.SceneManager.OnUnload += OnSceneUnload;
                NetworkManager.Singleton.SceneManager.OnUnloadComplete += OnSceneUnloadComplete;
                NetworkManager.Singleton.SceneManager.OnUnloadEventCompleted += OnSceneUnloadEventCompleted;

                // Debug.Log($"[{nameof(NetcodeManager)}]: Scene events registered", this);
            }
        }

        void OnSceneUnloadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Scene \"{sceneName}\" unloaded (loadSceneMode: {loadSceneMode}, clientsCompleted: {{ {string.Join(", ", clientsCompleted)} }}, clientsTimedOut: {{ {string.Join(", ", clientsTimedOut)} }})", this);

            for (int i = 0; i < clientsCompleted.Count; i++)
            {
                sceneUnloadings[clientsCompleted[i]] = new SceneUnloadInfo(sceneName)
                {
                    IsDone = true,
                };
            }

            for (int i = 0; i < clientsTimedOut.Count; i++)
            {
                sceneUnloadings[clientsTimedOut[i]] = new SceneUnloadInfo(sceneName)
                {
                    IsDone = true,
                    IsTimedOut = true,
                };
            }
        }

        void OnSceneUnloadComplete(ulong clientId, string sceneName)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Scene \"{sceneName}\" unloaded ... (clientId: {clientId})", this);

            sceneUnloadings[clientId] = new SceneUnloadInfo(sceneName)
            {
                IsDone = true,
            };
        }

        void OnSceneUnload(ulong clientId, string sceneName, AsyncOperation asyncOperation)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Unloading scene \"{sceneName}\" ... (clientId: {clientId}, asyncOperation: {asyncOperation})", this);

            sceneUnloadings[clientId] = new SceneUnloadInfo(sceneName, asyncOperation)
            {

            };
        }

        void OnSceneSynchronizeComplete(ulong clientId)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Scene synchronized (clientId: {clientId})", this);

            for (int i = sceneSynchronizations.Count - 1; i >= 0; i--)
            {
                if (sceneSynchronizations[i] == clientId)
                { sceneSynchronizations.RemoveAt(i); }
            }
        }

        void OnSceneSynchronize(ulong clientId)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Synchronizing scene ... (clientId: {clientId})", this);

            for (int i = sceneSynchronizations.Count - 1; i >= 0; i--)
            {
                if (sceneSynchronizations[i] == clientId)
                { return; }
            }
            sceneSynchronizations.Add(clientId);
        }

        void OnSceneLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            for (int i = 0; i < clientsCompleted.Count; i++)
            {
                sceneLoadings[clientsCompleted[i]] = new SceneLoadInfo(sceneName, loadSceneMode)
                {
                    IsDone = true,
                };
            }

            for (int i = 0; i < clientsTimedOut.Count; i++)
            {
                sceneLoadings[clientsTimedOut[i]] = new SceneLoadInfo(sceneName, loadSceneMode)
                {
                    IsDone = true,
                    IsTimedOut = true,
                };
            }

            Debug.Log($"[{nameof(NetcodeManager)}]: Scene \"{sceneName}\" loaded (loadSceneMode: {loadSceneMode}, clientsCompleted: {{ {string.Join(", ", clientsCompleted)} }}, clientsTimedOut: {{ {string.Join(", ", clientsTimedOut)} }})", this);
        }

        void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Scene \"{sceneName}\" loaded ... (clientId: {clientId}, loadSceneMode: {loadSceneMode})", this);

            sceneLoadings[clientId] = new SceneLoadInfo(sceneName, loadSceneMode)
            {
                IsDone = true,
            };
        }

        void OnSceneLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Loading scene \"{sceneName}\" ... (clientId: {clientId}, loadSceneMode: {loadSceneMode}, asyncOperation: {asyncOperation})", this);

            sceneLoadings[clientId] = new SceneLoadInfo(sceneName, loadSceneMode, asyncOperation)
            {

            };
        }

        void Singleton_OnServerStopped(bool isHost)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: {(isHost ? "Host" : "Server")} stopped", this);
            SceneManager.UnloadAllScenes();
        }

        void Singleton_OnServerStarted()
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Server started", this);
        }

        void Singleton_OnClientStopped(bool isHost)
        {
            if (isHost)
            {
                Debug.Log($"[{nameof(NetcodeManager)}]: Host stopped", this);
            }
            else
            {
                if (string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
                {
                    Debug.Log($"[{nameof(NetcodeManager)}]: Client stopped without any reason", this);
                }
                else
                {
                    Debug.Log($"[{nameof(NetcodeManager)}]: Client stopped, reason: \"{NetworkManager.Singleton.DisconnectReason}\"", this);
                }
            }
            SceneManager.UnloadAllScenes();
        }

        void Singleton_OnClientStarted()
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Client started", this);
        }

        void Singleton_OnClientDisconnectCallback(ulong clientId)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Client {clientId} disconnected", this);
            var v = FindObjectOfType<Game.UI.MenuRoom>();
            if (v != null) v.RefreshPlayerList();
        }

        void Singleton_OnClientConnectedCallback(ulong clientId)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Client #{clientId} connected", this);
            var v = FindObjectOfType<Game.UI.MenuRoom>();
            if (v != null) v.RefreshPlayerList();
        }
    }
}

namespace Networking
{
    static class NetcodeVariableSerializers
    {
        public static void Init()
        {
            UserNetworkVariableSerialization<ulong[]>.WriteValue = Write;
            UserNetworkVariableSerialization<ulong[]>.ReadValue = Read;
        }

        static void Write(FastBufferWriter writer, in ulong[] v)
        {
            if (v is null)
            { throw new System.NullReferenceException($"{nameof(v)} is null"); }
            writer.WriteValue((byte)v.Length);
            for (int i = 0; i < v.Length; i++)
            { writer.WriteValue(v[i]); }
        }

        static void Read(FastBufferReader reader, out ulong[] v)
        {
            reader.ReadValue(out byte length);
            v = new ulong[length];
            for (int i = 0; i < v.Length; i++)
            {
                reader.ReadValue(out ulong item);
                v[i] = item;
            }
        }
    }
}