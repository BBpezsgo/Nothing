using Messages;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR && false
using UnityEditor;
[CustomPropertyDrawer(typeof(NetcodeServices.ClientList))]
public class IngredientDrawerUIE : PropertyDrawer
{
    bool foldout;
    readonly float padding = 6;

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();

        return container;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float listHeight = 0f;
        if (foldout)
        {
            var targetObject = property.serializedObject.targetObject;
            var targetObjectClassType = targetObject.GetType();
            var field = targetObjectClassType.GetField(property.propertyPath);
            if (field != null)
            {
                var value = field.GetValue(targetObject) as NetcodeServices.ClientList;
                listHeight = (EditorGUIUtility.singleLineHeight * value.Count) + (padding * 2f);
            }
        }
        return EditorGUIUtility.singleLineHeight + listHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var targetObject = property.serializedObject.targetObject;
        var targetObjectClassType = targetObject.GetType();
        var field = targetObjectClassType.GetField(property.propertyPath);
        NetcodeServices.ClientList value = null;
        if (field != null)
        { value = field.GetValue(targetObject) as NetcodeServices.ClientList; }

        EditorGUI.BeginProperty(position, label, property);

        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        foldout = EditorGUI.BeginFoldoutHeaderGroup(position, foldout, label);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.IntField(new Rect(position.x + position.width - 45, position.y, 45, EditorGUIUtility.singleLineHeight), value.Count);
        EditorGUI.EndDisabledGroup();

        if (foldout)
        {
            if (field != null)
            {
                float y = EditorGUIUtility.singleLineHeight;

                GUI.Box(new Rect(position.x, position.y + y, position.width, (y * value.Count) + (padding * 2)), GUIContent.none);

                y += padding;

                for (int i = 0; i < value.Count; i++)
                {
                    var item = value.ElementAt(i);
                    var rect = EditorGUI.PrefixLabel(new Rect(position.x + padding, position.y + y, position.width - (padding * 2f), EditorGUIUtility.singleLineHeight), new GUIContent(item.Key.ToString()));
                    EditorGUI.LabelField(rect, item.Value.ToString());

                    y += EditorGUIUtility.singleLineHeight;
                }
            }
        }

        EditorGUI.EndFoldoutHeaderGroup();

        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}
#endif

public class NetcodeServices : NetworkBehaviour, IRemoteAccountProvider
{
    [Serializable]
    struct UserData
    {
        [SerializeField, ReadOnly] internal string DisplayName;
        [SerializeField, ReadOnly] internal string ID;
        [SerializeField, ReadOnly] internal ulong NetworkID;
    }

    [SerializeField, ReadOnly] NetworkVariable<int> playerCount = new();
    [SerializeField, ReadOnly] NetworkVariable<ulong> masterClient = new();
    [SerializeField, ReadOnly] NetworkVariable<NetworkString> selectedSceneName = new("none");

    [SerializeField, NonReorderable, ReadOnly] List<UserData> UserDatas = new();

    [Serializable]
    public class Client
    {
        public IRemoteAccountProvider.RemoteAccount Profile;
        public TimeSpan LastHeartbeat;
        public ulong NetworkID;

        public Client(ulong networkID)
        {
            Profile = null;
            LastHeartbeat = DateTime.Now.TimeOfDay;
            NetworkID = networkID;
        }

        public Client(IRemoteAccountProvider.RemoteAccount profile, ulong networkID)
        {
            Profile = profile;
            LastHeartbeat = DateTime.Now.TimeOfDay;
            NetworkID = networkID;
        }
    }

    public Dictionary<ulong, Client> Clients = new();

    [ReadOnly]
    [SerializeField] float timeToNextHeartbeat = 1f;

    public ulong MasterClientID => masterClient.Value;
    public int ClientCount => playerCount.Value;

    public string SelectedSceneName
    {
        get => selectedSceneName.Value == "none" ? null : selectedSceneName.Value;
        set => selectedSceneName.Value = value;
    }

#if UNITY_EDITOR
    [Header("Test")]
    [SerializeField] bool disableHeartbeat = false;
#endif

    public delegate void OnClientsUpdatedEventHandler();
    public event OnClientsUpdatedEventHandler OnClientsUpdated;
    string publicAddress;

    public string PublicAddress => publicAddress;

    public bool NeedAuthorization => false;

    void Start()
    {
        if (Services.Singleton != null)
        {
            Debug.Log($"[{nameof(NetcodeServices)}]: Instance already registered, destroying self");
            GameObject.Destroy(gameObject);
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.Log($"[{nameof(NetcodeServices)}]: NetworkManager is null, destroying self");
            GameObject.Destroy(gameObject);
            return;
        }

        Services.Singleton = this;

        StartCoroutine(GetPublicIP());

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private IEnumerator GetPublicIP()
    {
        var www = new UnityWebRequest("https://api.ipify.org")
        {
            downloadHandler = new DownloadHandlerBuffer()
        };

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.DataProcessingError || www.result == UnityWebRequest.Result.ProtocolError)
        { yield break; }

        publicAddress = www.downloadHandler.text;
    }

    void ResetServerInfo()
    {
        if (playerCount.CanClientWrite(NetworkManager.Singleton.LocalClientId)) playerCount.Value = Clients.Count;
        if (masterClient.CanClientWrite(NetworkManager.Singleton.LocalClientId)) masterClient.Value = NetworkManager.Singleton.LocalClientId;
    }

    void OnClientDisconnect(ulong id)
    {
        if (Clients.ContainsKey(id))
        { Clients.Remove(id); }
    }
    void OnClientConnected(ulong id)
    {
        if (Clients.ContainsKey(id))
        {
            Clients[id] = new Client(id);
        }
        else
        {
            Clients.Add(id, new Client(id));
            OnClientsUpdated?.Invoke();
        }
    }

    void FixedUpdate()
    {
        if (NetworkManager.Singleton.IsListening && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsConnectedClient))
        {
#if UNITY_EDITOR
            if (disableHeartbeat) return;
#endif
            timeToNextHeartbeat -= Time.fixedDeltaTime;
            if (timeToNextHeartbeat <= 0f)
            {
                timeToNextHeartbeat = 2f;
                if (IsServer)
                {
                    ResetServerInfo();

                    ProcessClient(NetworkManager.Singleton.LocalClientId, AuthManager.AuthProvider.ID);
                    OnClientInfoRecivedClientRpc(NetworkManager.Singleton.LocalClientId, AuthManager.AuthProvider.ID);
                }
                else
                {
                    OnClientInfoRecivedServerRpc(NetworkManager.Singleton.LocalClientId, AuthManager.AuthProvider.ID);
                }
            }
        }
        else
        {
            Clients.Clear();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void OnClientInfoRecivedServerRpc(ulong clientID, string ID)
    {
        ProcessClient(clientID, ID);
        OnClientInfoRecivedClientRpc(clientID, ID);
    }

    [ClientRpc]
    void OnClientInfoRecivedClientRpc(ulong clientID, string ID)
    {
        ProcessClient(clientID, ID);
    }

    void ProcessClient(ulong clientID, string ID)
    {
        if (clientID == NetworkManager.LocalClientId)
        {
            if (Clients.ContainsKey(NetworkManager.LocalClientId))
            {
                Clients[NetworkManager.LocalClientId] = new Client(new IRemoteAccountProvider.RemoteAccount(AuthManager.AuthProvider.DisplayName), NetworkManager.LocalClientId);
            }
            else
            {
                Clients.Add(NetworkManager.LocalClientId, new Client(new IRemoteAccountProvider.RemoteAccount(AuthManager.AuthProvider.DisplayName), NetworkManager.LocalClientId));
                OnClientsUpdated?.Invoke();
            }
            return;
        }
        if (AuthManager.RemoteAccountProvider == null) return;
        StartCoroutine(AuthManager.RemoteAccountProvider.GetAsync(ID, (result, error) =>
        {
            if (error != null) return;

            if (Clients.ContainsKey(clientID))
            {
                Clients[clientID] = new Client(result, clientID);
            }
            else
            {
                Clients.Add(clientID, new Client(result, clientID));
                OnClientsUpdated?.Invoke();
            }
        }));
    }

    internal void OnUserData(Messages.UserDataHeader message)
    {
        for (int i = 0; i < UserDatas.Count; i++)
        {
            if (UserDatas[i].NetworkID == message.Sender)
            {
                UserData userData = UserDatas[i];

                userData.DisplayName = message.UserName;
                userData.ID = message.ID;

                UserDatas[i] = userData;
                return;
            }
        }

        UserDatas.Add(new UserData()
        {
            DisplayName = message.UserName,
            ID = message.ID,
            NetworkID = message.Sender,
        });
    }

    public IRemoteAccountProvider.RemoteAccount Get(string userId)
    {
        for (int i = 0; i < UserDatas.Count; i++)
        {
            if (UserDatas[i].ID != userId) continue;
            return new IRemoteAccountProvider.RemoteAccount(UserDatas[i].DisplayName);
        }
        return null;
    }

    public IEnumerator GetAsync(string userId, Action<IRemoteAccountProvider.RemoteAccount, object> callback)
    {
        for (int i = 0; i < UserDatas.Count; i++)
        {
            if (UserDatas[i].ID != userId) continue;
            callback?.Invoke(new IRemoteAccountProvider.RemoteAccount(UserDatas[i].DisplayName), null);
            yield break;
        }

        if (IsServer)
        {
            NetcodeMessaging.BroadcastUnnamedMessage(new UserDataRequestHeader(MessageType.USER_DATA_REQUEST, NetworkManager.LocalClientId)
            {
                ID = userId,
            });
        }
        else
        {
            NetcodeMessaging.SendUnnamedMessage(new UserDataRequestHeader(MessageType.USER_DATA_REQUEST, NetworkManager.LocalClientId)
            {
                ID = userId,
            }, NetworkManager.ServerClientId);
        }

        yield return new WaitForSecondsRealtime(1f);

        TimeSpan started = DateTime.UtcNow.TimeOfDay;
        while ((DateTime.UtcNow.TimeOfDay - started).TotalSeconds < 5)
        {
            IRemoteAccountProvider.RemoteAccount possiblyFoundRemoteAccount = Get(userId);
            if (possiblyFoundRemoteAccount != null)
            {
                callback?.Invoke(possiblyFoundRemoteAccount, null);
                yield break;
            }

            yield return new WaitForSecondsRealtime(1f);
        }

        callback?.Invoke(null, "Timed out");
        yield break;
    }
}

namespace Unity.Netcode
{
    public static class Services
    {
        static NetcodeServices instance;

        public static NetcodeServices Singleton
        {
            get
            {
                return instance;
            }
            set
            {
                instance = value;
            }
        }
    }

    public struct Client : INetworkSerializable
    {
        public NetworkString Name;
        public int Id;
        public bool IsHost;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Id);
            serializer.SerializeValue(ref IsHost);
        }
    }
}