using Authentication;

using DataUtilities.Serializer;

using Networking.Components;
using Networking.Messages;
using Networking.Network;

using System;
using System.Collections.Generic;

using UI;

using Unity.Collections;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.Netcode;

using UnityEngine;

namespace Networking
{
    struct NetcodeRPC
    {
        internal uint NetworkID;
        internal byte ComponentIndex;
        internal string Method;
        internal byte[] Data;
    }

    [AddComponentMenu("Netcode/Synchronizer")]
    [RequireComponent(typeof(RuntimeNetStatsMonitor))]
    public class NetcodeSynchronizer : NetworkBehaviour
    {
        public static NetcodeSynchronizer Instance;

        [Serializable]
        public struct SpawnObject
        {
            public string PrefabID;
            public uint NetworkID;
            public Vector3 Position;

            public SpawnObject(string prefabID, uint networkID, Vector3 position)
            {
                if (string.IsNullOrWhiteSpace(prefabID))
                { throw new ArgumentException($"'{nameof(prefabID)}' cannot be null or whitespace.", nameof(prefabID)); }
                PrefabID = prefabID;
                NetworkID = networkID;
                Position = position;
            }
        }

        [Serializable]
        struct SentChunk
        {
            [SerializeField] internal ulong ID;
            [SerializeField] internal ulong SerialNumber;
            internal Action Acknowledged;
        }

        [Serializable]
        struct SentRequest
        {
            [SerializeField] internal ulong ID;
            [SerializeField, ProgressBar(0f, 1f, false, ShowPercent = true, LabelPosition = ProgressBarAttribute.PropertyLabelPosition.Left)] float progress;
            internal Action<byte[]> Callback;
            internal readonly float Progress => progress;

            readonly Action<ChunkCollector> ProgressCallback;

            public SentRequest(ulong id, Action<byte[]> onDone, Action<ChunkCollector> onProgress)
            {
                this.ID = id;
                this.Callback = onDone;
                this.ProgressCallback = onProgress;
                this.progress = 0f;
            }

            internal void OnProgress(ChunkCollector chunkCollector)
            {
                this.ProgressCallback?.Invoke(chunkCollector);
                this.progress = chunkCollector.Progress;
            }
        }

        [SerializeField] internal bool Logs = true;
        [SerializeField, NonReorderable, ReadOnly] List<NetcodeView> ObservedObjects = new();
        [SerializeField, Button(nameof(SpawnTestObject), false, true, "Spawn Test Object")] string btn0;
        [SerializeField, Button(nameof(LoadTestScene), false, true, "Load Test Scene")] string btn1;
        [SerializeField, ReadOnly] uint NetworkIdCounter = 0;
        [SerializeField, NonReorderable, ReadOnly] List<SpawnObject> SpawnQueue = new();
        [SerializeField, NonReorderable, ReadOnly] Queue<NetcodeRPC> RpcQueue = new();
        [SerializeField, ReadOnly] string SceneName = null;
        [SerializeField, ReadOnly] bool SceneLoaded = false;
        [SerializeField, NonReorderable, ReadOnly] List<uint> ClientsLoadingScene = new();
        [SerializeField, ReadOnly] ulong RequestIdCounter = 0;
        [SerializeField, NonReorderable, ReadOnly] List<SentRequest> SentRequests = new();
        [SerializeField, NonReorderable, ReadOnly] List<SentChunk> SentChunks = new();
        [SerializeField, ReadOnly] ChunkCollectorManager ChunkCollector = new();

        [SerializeField] ImguiWindow Window;

        [SerializeField] List<GameObject> NetworkPrefabs = new();

        const int CHUNK_SIZE = 1024;
        public uint SyncPerSecond = 5;

        [SerializeField, ReadOnly] float GeneralSyncTimer;
        [SerializeField, ReadOnly] float RateSyncTimer;

        void LoadTestScene() => LoadScene(AssetManager.AssetManager.Instance.settings.test_scene);
        void SpawnTestObject() => SpawnPrefab(AssetManager.AssetManager.Instance.settings.test_object, Vector3.zero, true);

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning($"[{nameof(NetcodeSynchronizer)}] Instance already registered, destroying this gameObject");
                gameObject.Destroy();
                return;
            }
            Instance = this;

            Window = IMGUIManager.Instance.CreateWindow(new Rect(10f, 10f, 150f, 20f));
            Window.Title = "Synchronizer";
            Window.DrawContent = DrawWindowContent;
        }

        void DrawWindowContent()
        {
            GUILayout.Label("Sent Requests");
            for (int i = 0; i < SentRequests.Count; i++)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"ID: {SentRequests[i].ID}");

                Rect progressBarRect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, 18f, GUI.skin.GetStyle("progress-bar-bg"));

                GUI.Box(progressBarRect, GUIContent.none, GUI.skin.GetStyle("progress-bar-bg"));

                if (SentRequests[i].Progress > float.Epsilon)
                {
                    Rect progressBarFgRect = progressBarRect;
                    progressBarFgRect.width *= Maths.Clamp01(SentRequests[i].Progress);
                    GUI.Box(progressBarFgRect, GUIContent.none, GUI.skin.GetStyle("progress-bar-fg"));
                }

                GUI.Label(progressBarRect, $"{SentRequests[i].Progress:P}");

                GUILayout.EndVertical();
            }
            GUILayout.Label("Sent Chunks");
            for (int i = 0; i < SentChunks.Count; i++)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"ID: {SentChunks[i].ID}");
                GUILayout.Label($"SerialNumber: {SentChunks[i].SerialNumber}");
                GUILayout.EndVertical();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void Start()
        {
            NetcodeView[] objects = GameObject.FindObjectsOfType<NetcodeView>(false);
            for (int i = 0; i < objects.Length; i++)
            { RegisterObjectInstance(objects[i].gameObject, objects[i].gameObject.name, objects[i].transform.position, false); }
        }

        void OnEnable()
        {
            GeneralSyncTimer = 0f;
            RateSyncTimer = 0f;
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.F2)) SpawnTestObject();
            else if (Input.GetKeyUp(KeyCode.F3))
            {
                var comp = GetComponent<RuntimeNetStatsMonitor>();
                comp.Visible = !comp.Visible;
            }

            GeneralSyncTimer += Time.deltaTime;

            if (GeneralSyncTimer >= (float)(1f / SyncPerSecond))
            {
                GeneralSyncTimer = 0;
                this.SyncComponents();
            }

            if (NetworkManager.IsServer)
            {
                RateSyncTimer += Time.deltaTime;
                if (RateSyncTimer >= 5f)
                {
                    RateSyncTimer = 0;
                    NetcodeMessaging.BroadcastUnnamedMessage(new LiteralHeader<byte>(MessageType.RATE, NetworkManager.LocalClientId)
                    {
                        Value = (byte)SyncPerSecond,
                    });
                }
            }
        }

        void FixedUpdate()
        {
            for (int i = SpawnQueue.Count - 1; i >= 0; i--)
            {
                SpawnObject spawnObject = SpawnQueue[i];
                SpawnPrefab(spawnObject.PrefabID, spawnObject.Position, spawnObject.NetworkID, IsServer);
                SpawnQueue.RemoveAt(i);
            }
        }

        public void RPC(MonoBehaviour component, string methodName, Action<FastBufferWriter> callback)
        {
            using FastBufferWriter writer = new(NetcodeMessaging.MessageSize, Allocator.Temp);
            callback?.Invoke(writer);
            RPC(component, methodName, writer.ToArray());
        }
        public void RPC(MonoBehaviour component, string methodName) => RPC(component, methodName, new byte[0]);
        public void RPC(MonoBehaviour component, string methodName, byte[] data) => RPC(component, component.gameObject, methodName, data);
        public void RPC(MonoBehaviour component, GameObject obj, string methodName, byte[] data) => RPC(component, obj.GetComponent<NetcodeView>(), methodName, data);
        public void RPC(MonoBehaviour component, NetcodeView obj, string methodName, byte[] data)
        {
            Component[] components = obj.ObservedComponents;
            for (int i = 0; i < components.Length; i++)
            {
                Component _component = components[i];
                if (_component != component) continue;

                RpcQueue.Enqueue(new NetcodeRPC()
                {
                    NetworkID = obj.ID,
                    ComponentIndex = (byte)i,
                    Method = methodName,
                    Data = data,
                });
                break;
            }
        }

        void SyncComponents()
        {
            if (NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.IsListening) return;
            if (IsServer)
            {
                using FastBufferWriter writer = new(NetcodeMessaging.MessageSize, Allocator.Temp);
                for (int i = 0; i < ObservedObjects.Count; i++)
                {
                    for (int j = 0; j < ObservedObjects[i].ObservedComponents.Length; j++)
                    {
                        if (ObservedObjects[i] == null) continue;
                        if (ObservedObjects[i].ObservedNetworkComponents[j] == null) continue;
                        try
                        {
                            Serializer serializer = new();
                            ObservedObjects[i].ObservedNetworkComponents[j].OnSerializeView(null, serializer, new NetcodeMessageInfo(true, default));

                            ComponentHeader componentHeader = new(MessageType.SYNC, NetworkManager.LocalClientId)
                            {
                                ObjectID = ObservedObjects[i].ID,
                                ComponentIndex = (byte)j,
                                Data = serializer.Result,
                            };
                            if (!writer.TryBeginWrite(componentHeader.Size()))
                            { throw new OverflowException("Not enough space in the buffer"); }
                            componentHeader.Serialize(writer);
                        }
                        catch (Exception exception)
                        { Debug.LogException(exception); }
                    }
                }
                if (writer.Length > 0)
                { NetcodeMessaging.BroadcastUnnamedMessage(writer, NetworkDelivery.UnreliableSequenced); }
            }
            {
                using FastBufferWriter writer = new(NetcodeMessaging.MessageSize, Allocator.Temp);
                while (RpcQueue.TryDequeue(out NetcodeRPC rpc))
                {
                    RpcHeader rpcHeader = new(MessageType.RPC, NetworkManager.LocalClientId)
                    {
                        ComponentIndex = rpc.ComponentIndex,
                        Data = rpc.Data,
                        MethodName = rpc.Method,
                        ObjectID = rpc.NetworkID,
                    };
                    if (!writer.TryBeginWrite(rpcHeader.Size()))
                    { throw new OverflowException("Not enough space in the buffer"); }
                    rpcHeader.Serialize(writer);
                }
                if (writer.Length > 0)
                {
                    if (IsServer)
                    {
                        NetcodeMessaging.BroadcastUnnamedMessage(writer, NetworkDelivery.ReliableSequenced);
                    }
                    else
                    {
                        NetcodeMessaging.SendUnnamedMessage(writer, NetworkManager.ServerClientId, NetworkDelivery.ReliableSequenced);
                    }
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (Logs) Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Register events");

            if (NetworkManager.CustomMessagingManager != null)
            { NetworkManager.CustomMessagingManager.OnUnnamedMessage += OnReceivedUnnamedMessage; }

            if (IsServer)
            { NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback; }
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager == null) return;

            if (Logs) Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Unregister events");

            if (NetworkManager.CustomMessagingManager != null)
            { NetworkManager.CustomMessagingManager.OnUnnamedMessage -= OnReceivedUnnamedMessage; }
            NetworkManager.OnClientDisconnectCallback -= OnClientConnectedCallback;
        }

        void OnClientConnectedCallback(ulong clientId)
        {

        }

        NetcodeView GetNetworkObject(uint networkId)
        {
            for (int i = 0; i < ObservedObjects.Count; i++)
            {
                if (ObservedObjects[i].ID != networkId) continue;
                return ObservedObjects[i];
            }
            return null;
        }
        bool TryGetNetworkObject(uint networkId, out NetcodeView networkObject)
        {
            networkObject = GetNetworkObject(networkId);
            return networkObject != null;
        }

        void OnReceivedUnnamedMessage(ulong clientId, FastBufferReader reader)
        {
            if (Logs) Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Received {reader.Length} bytes from client {clientId}");

            var messages = NetcodeMessaging.ReciveUnnamedMessage(clientId, reader);

            for (int i = 0; i < messages.Length; i++)
            {
                BaseMessage baseMessage = messages[i];

                switch (baseMessage.Type)
                {
                    case MessageType.SYNC:
                        {
                            if (!isActiveAndEnabled) return;
                            ComponentHeader message = (ComponentHeader)baseMessage;
                            if (!TryGetNetworkObject(message.ObjectID, out var networkObject))
                            {
                                Debug.Log($"Network object {message.ObjectID} not found; request prefab info");
                                NetcodeMessaging.SendUnnamedMessage(new ObjectHeader(MessageType.UNKNOWN_OBJECT, NetworkManager.LocalClientId)
                                {
                                    ObjectID = message.ObjectID,
                                }, NetworkManager.ServerClientId);
                                break;
                            }

                            if (message.ComponentIndex >= networkObject.ObservedNetworkComponents.Length ||
                                message.ComponentIndex < 0)
                            {
                                Debug.LogError($"Component {message.ComponentIndex} not found in object {networkObject}", networkObject);
                                break;
                            }

                            try
                            {
                                networkObject.ObservedNetworkComponents[message.ComponentIndex].OnSerializeView(new Deserializer(message.Data), null, new NetcodeMessageInfo(false, message.Sent));
                            }
                            catch (Exception ex)
                            { Debug.LogException(ex); }
                            break;
                        }
                    case MessageType.UNKNOWN_OBJECT:
                        {
                            if (!isActiveAndEnabled) return;
                            ObjectHeader message = (ObjectHeader)baseMessage;
                            for (int j = 0; j < ObservedObjects.Count; j++)
                            {
                                if (ObservedObjects[j] == null) continue;
                                if (ObservedObjects[j].ID != message.ObjectID) continue;
                                if (ObservedObjects[j].gameObject == null)
                                {
                                    if (Logs) Debug.Log($"Sending info that \"{ObservedObjects[j].ID}\" is destroyed");
                                    NetcodeMessaging.SendUnnamedMessage(new ObjectHeader(MessageType.DESTROY_OBJECT, NetworkManager.LocalClientId)
                                    {
                                        ObjectID = ObservedObjects[j].ID,
                                    }, message.Sender);
                                }
                                else
                                {
                                    if (Logs) Debug.Log($"Sending prefab info '{ObservedObjects[j].gameObject.name}'");
                                    NetcodeMessaging.SendUnnamedMessage(new InstantiationHeader(MessageType.SPAWN_OBJECT, NetworkManager.LocalClientId)
                                    {
                                        PrefabName = ObservedObjects[j].gameObject.name,
                                        NetworkID = ObservedObjects[j].ID,
                                        Position = ObservedObjects[j].transform.position,
                                    }, message.Sender);
                                }
                                return;
                            }
                            Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Object {message.ObjectID} not found, sending DESTROY_OBJECT message");
                            NetcodeMessaging.SendUnnamedMessage(new ObjectHeader(MessageType.DESTROY_OBJECT, NetworkManager.LocalClientId)
                            {
                                ObjectID = message.ObjectID,
                            }, message.Sender);
                            break;
                        }

                    case MessageType.SPAWN_OBJECT:
                        {
                            if (!isActiveAndEnabled) return;
                            InstantiationHeader message = (InstantiationHeader)baseMessage;
                            if (NetworkManager.IsServer)
                            { break; }
                            for (int j = 0; j < SpawnQueue.Count; j++)
                            { if (SpawnQueue[j].NetworkID == message.NetworkID) goto ExitLoop; }
                            SpawnQueue.Add(new SpawnObject(message.PrefabName, message.NetworkID, message.Position));
                        ExitLoop:
                            break;
                        }
                    case MessageType.GET_RATE:
                        {
                            NetcodeMessaging.SendUnnamedMessage(new LiteralHeader<byte>(MessageType.RATE, NetworkManager.LocalClientId)
                            {
                                Value = (byte)SyncPerSecond,
                            }, baseMessage.Sender);
                            break;
                        }
                    case MessageType.RATE:
                        {
                            if (NetworkManager.IsServer)
                            { break; }
                            LiteralHeader<byte> message = (LiteralHeader<byte>)baseMessage;
                            SyncPerSecond = message.Value;
                            break;
                        }

                    case MessageType.DESTROY_OBJECT:
                        {
                            if (!isActiveAndEnabled) return;
                            ObjectHeader message = (ObjectHeader)baseMessage;
                            if (Logs) Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Destroying object {message.ObjectID}");
                            if (TryGetNetworkObject(message.ObjectID, out NetcodeView obj))
                            { GameObject.Destroy(obj.gameObject); }
                            break;
                        }
                    case MessageType.RPC:
                        {
                            if (!isActiveAndEnabled) return;
                            RpcHeader message = (RpcHeader)baseMessage;
                            if (IsServer)
                            {
                                RpcQueue.Enqueue(new NetcodeRPC()
                                {
                                    ComponentIndex = message.ComponentIndex,
                                    Method = message.MethodName,
                                    NetworkID = message.ObjectID,
                                    Data = message.Data,
                                });
                            }

                            OnRpcRecived(message);
                            break;
                        }

                    case MessageType.GET_SCENE:
                        {
                            if (!isActiveAndEnabled) return;
                            BaseMessage message = (BaseMessage)baseMessage;
                            if (!IsServer) break;

                            if (!ClientsLoadingScene.Contains(message.Sender)) ClientsLoadingScene.Add(message.Sender);
                            NetcodeMessaging.SendUnnamedMessage(new StringHeader(MessageType.SCENE, NetworkManager.LocalClientId)
                            {
                                Value = SceneName ?? "null",
                            }, message.Sender);

                            break;
                        }

                    case MessageType.SCENE:
                        {
                            if (!isActiveAndEnabled) return;
                            if (!IsClient) break;

                            StringHeader message = (StringHeader)baseMessage;
                            SceneName = (message.Value == "null") ? null : message.Value;
                            if (!SceneLoaded)
                            {
                                LoadScene(SceneName);
                            }

                            break;
                        }

                    case MessageType.SCENE_LOADED:
                        {
                            if (!isActiveAndEnabled) return;
                            BaseMessage message = (BaseMessage)baseMessage;
                            if (ClientsLoadingScene.Contains(message.Sender)) ClientsLoadingScene.Remove(message.Sender);

                            break;
                        }

                    case MessageType.CHUNK:
                        {
                            ChunkHeader message = (ChunkHeader)baseMessage;

                            NetcodeMessaging.SendUnnamedMessage(new EmptyChunkHeader(MessageType.CHUNK_ACK, NetworkManager.LocalClientId)
                            {
                                ID = message.ID,
                                SerialNumber = message.SerialNumber,
                            }, message.Sender);

                            byte[] finishedTransition = ChunkCollector.Receive(message, out var chunkCollector);

                            for (int j = SentRequests.Count - 1; j >= 0; j--)
                            {
                                if (SentRequests[j].ID != message.ID) continue;
                                SentRequests[j].OnProgress(chunkCollector);
                            }

                            if (finishedTransition == null) break;

                            Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Transition {message.ID} finished ({finishedTransition.Length} bytes of {message.TotalSize} bytes)");

                            for (int j = SentRequests.Count - 1; j >= 0; j--)
                            {
                                if (SentRequests[j].ID != message.ID) continue;
                                SentRequests[j].Callback?.Invoke(finishedTransition);
                                SentRequests.RemoveAt(j);
                            }
                            break;
                        }

                    case MessageType.CHUNK_ACK:
                        {
                            EmptyChunkHeader message = (EmptyChunkHeader)baseMessage;
                            for (int j = SentChunks.Count - 1; j >= 0; j--)
                            {
                                if (SentChunks[j].ID != message.ID ||
                                    SentChunks[j].SerialNumber != message.SerialNumber) continue;

                                try
                                { SentChunks[j].Acknowledged?.Invoke(); }
                                catch (Exception exception)
                                { Debug.LogException(exception); }

                                SentChunks.RemoveAt(j);
                                break;
                            }
                            break;
                        }

                    case MessageType.REQUEST:
                        {
                            RequestHeader message = (RequestHeader)baseMessage;
                            HandleRequest(message.Value, message.ID, message.Sender);
                            break;
                        }

                    case MessageType.USER_DATA_REQUEST:
                        {
                            UserDataRequestHeader message = (UserDataRequestHeader)baseMessage;

                            if (AuthManager.AuthProvider != null && AuthManager.AuthProvider.IsAuthorized && AuthManager.AuthProvider.ID == message.ID)
                            {
                                NetcodeMessaging.SendUnnamedMessage(new UserDataHeader(MessageType.USER_DATA, NetworkManager.LocalClientId)
                                {
                                    UserName = AuthManager.AuthProvider.DisplayName,
                                    ID = AuthManager.AuthProvider.ID,
                                }, baseMessage.Sender);
                                break;
                            }

                            if (AuthManager.RemoteAccountProvider != null)
                            {
                                var result = AuthManager.RemoteAccountProvider.Get(message.ID);
                                if (result != null)
                                {
                                    NetcodeMessaging.SendUnnamedMessage(new UserDataHeader(MessageType.USER_DATA, NetworkManager.LocalClientId)
                                    {
                                        UserName = result.DisplayName,
                                        ID = message.ID,
                                    }, baseMessage.Sender);
                                }
                            }

                            break;
                        }

                    case MessageType.USER_DATA:
                        {
                            UserDataHeader message = (UserDataHeader)baseMessage;
                            Services.Singleton.OnUserData(message);
                            break;
                        }

                    case MessageType.USER_DATA_REQUEST_DIRECT:
                        {
                            if (AuthManager.AuthProvider != null && AuthManager.AuthProvider.IsAuthorized)
                            {
                                NetcodeMessaging.SendUnnamedMessage(new UserDataHeader(MessageType.USER_DATA, NetworkManager.LocalClientId)
                                {
                                    UserName = AuthManager.AuthProvider.DisplayName,
                                    ID = AuthManager.AuthProvider.ID,
                                }, baseMessage.Sender);
                                break;
                            }

                            break;
                        }

                    case MessageType.UNKNOWN:
                    default:
                        throw new Exception($"Unknown baseMessage type {baseMessage.Type}({baseMessage.TypeRaw}) form client {baseMessage.Sender}");
                }
            }
        }

        void HandleRequest(string _r, ulong messageID, uint sender)
        {
            string request = _r.Split(':')[0];
            string data = _r[(request.Length + 1)..];

            switch (request)
            {
                case "assets":
                    {
                        if (AssetManager.AssetManager.Instance.Assets.Root is not AssetManager.RealFolder)
                        { Debug.LogWarning($"Full-assets packing only works properly on real folders."); }

                        Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Packing full-assets ...");

                        DataUtilities.FilePacker.Packer packer = new(new DataUtilities.FilePacker.PackHeader()
                        { SaveMetadata = false, });

                        byte[] result = packer.Pack(AssetManager.AssetManager.Instance.Assets.Root);

                        if (result == null)
                        { Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Packing failed"); }
                        else
                        { Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Full-assets packed"); }

                        Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Sending big data ({(result == null ? 0 : result.Length)}) bytes)");
                        SendBigData(sender, messageID, result ?? new byte[0]);
                        return;
                    }
                case "assets_file":
                    {
                        Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Find file \"{data}\" ...");
                        var result = AssetManager.AssetManager.Instance.Assets.GetAbsoluteFile(data);
                        if (result == null)
                        {
                            Debug.Log($"[{nameof(NetcodeSynchronizer)}]: File \"{data}\" not found");
                        }
                        else
                        {
                            Debug.Log($"[{nameof(NetcodeSynchronizer)}]: File \"{data}\" found");
                        }
                        Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Sending big data ({(result == null ? 0 : result.Bytes)}) bytes)");
                        SendBigData(sender, messageID, result == null ? new byte[0] : result.Bytes);
                        return;
                    }
                case "assets_folder":
                    {
                        Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Find folder \"{data}\" ...");
                        var folder = AssetManager.AssetManager.Instance.Assets.GetAbsoluteFolder(data);
                        string result = "";
                        if (folder != null)
                        {
                            foreach (var folder_ in folder.Folders)
                            { result += folder_.Name + "\r\n"; }
                            foreach (var file in folder.Files)
                            { result += file.Name + "\r\n"; }
                            Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Folder \"{data}\" found");
                        }
                        else
                        {
                            Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Folder \"{data}\" not found");
                        }
                        byte[] _result = System.Text.Encoding.ASCII.GetBytes(result);
                        Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Sending big data ({_result.Length}) bytes)");
                        SendBigData(sender, messageID, _result);
                        return;
                    }
                default: throw new NotImplementedException($"Request \"{request}\" not implemented");
            }
        }

        public void SendBigData<T>(ulong destination, ulong id, ISerializable<T> data)
            where T : ISerializable<T>
            => SendBigData(destination, id, SerializerStatic.Serialize(data));
        public void SendBigData(ulong destination, ulong id, byte[] data)
            => StartCoroutine(StartSendBigData(destination, id, data.Chunks(CHUNK_SIZE), data.Length));

        public void SendBigData<T>(ulong destination, ulong id, ISerializable<T> data, Action callback)
            where T : ISerializable<T>
            => SendBigData(destination, id, SerializerStatic.Serialize(data), callback);
        public void SendBigData(ulong destination, ulong id, byte[] data, Action callback)
            => StartCoroutine(StartSendBigData(destination, id, data.Chunks(CHUNK_SIZE), data.Length, callback));

        System.Collections.IEnumerator StartSendBigData(ulong destination, ulong id, byte[][] chunks, int totalSize)
        { yield return StartSendBigData(destination, id, chunks, totalSize, () => { }); }
        System.Collections.IEnumerator StartSendBigData(ulong destination, ulong id, byte[][] chunks, int totalSize, Action callback)
        {
            Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Start sending chunks ({chunks.Length} chunk) ({totalSize} bytes total)");

            for (int i = 0; i < chunks.Length; i++)
            {
                byte[] data = chunks[i];
                ulong serialNumber = (ulong)i;

                yield return new WaitForSecondsRealtime(.1f);
                yield return StartSendChunk(destination, id, serialNumber, totalSize, data);
            }

            try
            { callback?.Invoke(); }
            catch (Exception exception)
            { Debug.LogError(exception); }

            yield break;
        }

        System.Collections.IEnumerator StartSendChunk(ulong destination, ulong id, ulong serialNumber, int totalSize, byte[] chunk)
        {
            bool acknowledged = false;

            SendChunk(destination, id, serialNumber, totalSize, chunk, () =>
            { acknowledged = true; });

            while (!acknowledged)
            { yield return new WaitForFixedUpdate(); }

            yield break;
        }

        void SendChunk(ulong destination, ulong id, ulong serialNumber, int totalSize, byte[] chunk, Action onAcknowledged)
        {
            SentChunks.Add(new SentChunk()
            {
                Acknowledged = onAcknowledged,
                ID = id,
                SerialNumber = serialNumber,
            });
            Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Send chunk {serialNumber} ({chunk.Length} bytes) to Client {destination}");
            NetcodeMessaging.SendUnnamedMessage(new ChunkHeader(MessageType.CHUNK, NetworkManager.LocalClientId)
            {
                ID = id,
                SerialNumber = serialNumber,
                Chunk = chunk,
                TotalSize = totalSize,
                ChunkSize = CHUNK_SIZE,
            }, destination);
        }

        internal System.Collections.IEnumerator SendRequestAsync(ulong destination, string data, Action<Network.ChunkCollector> progress, Action<byte[]> callback)
        {
            bool received = false;

            SendRequest(destination, data, progress, response =>
            {
                received = true;
                callback?.Invoke(response);
            });

            while (!received)
            { yield return new WaitForSecondsRealtime(.5f); }

            yield break;
        }
        internal void SendRequest(ulong destination, string data, Action<Network.ChunkCollector> progress, Action<byte[]> callback)
        {
            ulong id = RequestIdCounter++;
            SentRequests.Add(new SentRequest(id, callback, progress));
            NetcodeMessaging.SendUnnamedMessage(new RequestHeader(MessageType.REQUEST, NetworkManager.LocalClientId)
            {
                ID = id,
                Value = data,
            }, destination);
        }

        void OnRpcRecived(RpcHeader header)
        {
            if (!TryGetNetworkObject(header.ObjectID, out var obj))
            {
                Debug.LogError($"Network object {header.ObjectID} not found");
                return;
            }
            obj.OnRpcRecived(header);
        }

        public void LoadScene(string name)
        {
            SceneName = name;
            AssetManager.AssetManager.LoadScene(name, (prefabName, spawnOverNetwork, position, rotation) => SpawnPrefab(prefabName, position, false));
            SceneLoaded = true;
        }

        public GameObject SpawnPrefab(string prefabName, Vector3 position, bool broadcastSpawn) => SpawnPrefab(prefabName, position, ++NetworkIdCounter, broadcastSpawn);
        GameObject SpawnPrefab(string prefabName, Vector3 position, uint networkId, bool broadcastSpawn)
        {
            if (TryGetNetworkObject(networkId, out _))
            {
                Debug.LogWarning($"Network prefab {prefabName} with id {networkId} already spawned");
                return null;
            }
            Debug.Log($"Spawning network prefab '{prefabName}' with network id {networkId}");

            GameObject newObject = null;
            for (int i = 0; i < NetworkPrefabs.Count; i++)
            {
                if (NetworkPrefabs[i].name == prefabName)
                {
                    if (Game.ObjectGroups.Game != null)
                    { newObject = GameObject.Instantiate(NetworkPrefabs[i], position, Quaternion.identity, Game.ObjectGroups.Game); }
                    else
                    { newObject = GameObject.Instantiate(NetworkPrefabs[i], position, Quaternion.identity); }
                }
            }

            if (newObject == null)
            { newObject = AssetManager.AssetManager.InstantiatePrefab(prefabName, false, position, Quaternion.identity); }

            if (newObject == null)
            {
                Debug.LogError($"Failed to spawn prefab \"{prefabName}\"");
                return null;
            }

            newObject.name = prefabName;
            RegisterObjectInstance(newObject, prefabName, position, networkId, broadcastSpawn);

            return newObject;
        }
        public void RegisterObjectInstance(GameObject @object, string prefabName, Vector3 position, bool broadcastSpawn)
            => RegisterObjectInstance(@object, prefabName, position, ++NetworkIdCounter, broadcastSpawn);
        void RegisterObjectInstance(GameObject @object, string prefabName, Vector3 position, uint networkId, bool broadcastSpawn)
        {
            if (!isActiveAndEnabled) return;
            if (TryGetNetworkObject(networkId, out _))
            {
                Debug.LogWarning($"Network object instance {@object} already registered");
                return;
            }
            Debug.Log($"Registering network object instance '{@object}' with networkID {networkId}");
            NetcodeView netcodeView = @object.GetComponent<NetcodeView>();
            netcodeView.ID = networkId;
            ObservedObjects.Add(netcodeView);
            netcodeView.IsRegistered = true;
            if (NetworkManager.IsServer && broadcastSpawn)
            {
                Debug.Log($"Broadcast network object instance {networkId} registration");
                NetcodeMessaging.BroadcastUnnamedMessage(new InstantiationHeader(MessageType.SPAWN_OBJECT, NetworkManager.LocalClientId)
                {
                    NetworkID = networkId,
                    PrefabName = prefabName,
                    Position = position,
                });
            }
        }

        public void DestroyObject(uint networkId)
        {
            if (!isActiveAndEnabled) return;
            if (!NetworkManager.IsServer)
            { throw new PermissionException($"Client can not destroy object"); }

            if (!TryGetNetworkObject(networkId, out var networkObject))
            {
                Debug.LogError($"Network object {networkId} not found");
                return;
            }

            NetcodeMessaging.BroadcastUnnamedMessage(new ObjectHeader(MessageType.DESTROY_OBJECT, NetworkManager.LocalClientId)
            {
                ObjectID = networkId,
            });

            networkObject.gameObject.Destroy();
        }
    }

    [Serializable]
    public class PermissionException : Exception
    {
        public PermissionException() { }
        public PermissionException(string message) : base(message) { }
        public PermissionException(string message, Exception inner) : base(message, inner) { }
        protected PermissionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public interface INetworkObservable
    {
        internal void OnRPC(RpcHeader header);
        internal void OnSerializeView(Deserializer deserializer, Serializer serializer, NetcodeMessageInfo messageInfo);
    }

    static class Extensions
    {
        static NetcodeView GetNetcodeView(this INetworkObservable self)
        {
            if (self is MonoBehaviour monoBehaviour)
            {
                return monoBehaviour.GetComponent<NetcodeView>();
            }
            return null;
        }
    }
}
