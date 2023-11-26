using System;
using System.Collections;
using System.Collections.Generic;
using Authentication;
using Networking;
using Networking.Messages;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using Utilities;

#nullable enable

namespace Networking
{
    public class NetcodeServices : NetworkBehaviour, IRemoteAccountProvider, IRemoteAccountProviderWithCustomID<ulong>
    {
        [Serializable]
        struct UserData
        {
            [SerializeField, ReadOnly] internal string? DisplayName;
            [SerializeField, ReadOnly] internal string ID;
            [SerializeField, ReadOnly] internal ulong NetworkID;
        }

        [SerializeField, ReadOnly] NetworkVariable<int> playerCount = new();
        [SerializeField, ReadOnly] NetworkVariable<ulong> masterClient = new();

        [SerializeField, NonReorderable, ReadOnly] List<UserData> UserDatas = new();

        [Serializable]
        public class Client
        {
            public IRemoteAccountProvider.RemoteAccount? Profile;
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

#if UNITY_EDITOR
        [Header("Test")]
        [SerializeField] bool disableHeartbeat = false;
#endif

        public delegate void OnClientsUpdatedEventHandler();
        public event OnClientsUpdatedEventHandler? OnClientsUpdated;
        string? publicAddress;

        public string? PublicAddress => publicAddress;

        public bool NeedAuthorization => false;

        public bool CanRequestRemoteAccount => true;

        void Start()
        {
            if (Services.Singleton != null)
            {
                Debug.Log($"[{nameof(NetcodeServices)}]: Instance already registered, destroying self");
                GameObject.Destroy(gameObject);
                return;
            }

            /*
            if (NetworkManager.Singleton == null)
            {
                Debug.Log($"[{nameof(NetcodeServices)}]: NetworkManager is null, destroying self");
                GameObject.Destroy(gameObject);
                return;
            }
            */

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

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.DataProcessingError ||
                www.result == UnityWebRequest.Result.ProtocolError)
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
            if (IsServer)
            {
                Debug.Log($"[{nameof(NetcodeServices)}]: Client #{id} connected, sending USER_DATA_REQUEST_DIRECT ...");
                NetcodeMessaging.SendUnnamedMessage(new EmptyHeader(new MessageHeader(MessageType.USER_DATA_REQUEST_DIRECT, NetworkManager.LocalClientId)), id, NetworkDelivery.Reliable);
            }

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
                        OnClientInfoReceivedClientRpc(NetworkManager.Singleton.LocalClientId, AuthManager.AuthProvider.ID);
                    }
                    else
                    {
                        OnClientInfoReceivedServerRpc(NetworkManager.Singleton.LocalClientId, AuthManager.AuthProvider!.ID);
                    }
                }
            }
            else
            {
                Clients.Clear();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void OnClientInfoReceivedServerRpc(ulong clientID, string ID)
        {
            ProcessClient(clientID, ID);
            OnClientInfoReceivedClientRpc(clientID, ID);
        }

        [ClientRpc]
        void OnClientInfoReceivedClientRpc(ulong clientID, string ID)
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
            StartCoroutine(AuthManager.RemoteAccountProvider.GetAsync(ID, result =>
            {
                if (result.IsFailed) return;

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
                    userData.ID = message.ID!;

                    UserDatas[i] = userData;
                    return;
                }
            }

            UserDatas.Add(new UserData()
            {
                DisplayName = message.UserName,
                ID = message.ID!,
                NetworkID = message.Sender,
            });
        }

        public IRemoteAccountProvider.RemoteAccount? Get(string userId)
        {
            for (int i = 0; i < UserDatas.Count; i++)
            {
                UserData userData = UserDatas[i];
                if (userData.ID != userId) continue;
                return new IRemoteAccountProvider.RemoteAccount(userData.DisplayName);
            }
            return null;
        }

        public IEnumerator GetAsync(string userId, Action<TaskResult<IRemoteAccountProvider.RemoteAccount, string>> callback)
        {
            for (int i = 0; i < UserDatas.Count; i++)
            {
                UserData userData = UserDatas[i];
                if (userData.ID != userId) continue;
                callback?.Invoke(new IRemoteAccountProvider.RemoteAccount(userData.DisplayName));
                yield break;
            }

            if (IsServer)
            {
                NetcodeMessaging.BroadcastUnnamedMessage(new UserDataRequestHeader(new MessageHeader(MessageType.USER_DATA_REQUEST, NetworkManager.LocalClientId))
                {
                    ID = userId,
                });
            }
            else
            {
                NetcodeMessaging.SendUnnamedMessage(new UserDataRequestHeader(new MessageHeader(MessageType.USER_DATA_REQUEST, NetworkManager.LocalClientId))
                {
                    ID = userId,
                }, NetworkManager.ServerClientId);
            }

            yield return new WaitForSecondsRealtime(1f);

            TimeSpan started = DateTime.UtcNow.TimeOfDay;
            while ((DateTime.UtcNow.TimeOfDay - started).TotalSeconds < 5)
            {
                IRemoteAccountProvider.RemoteAccount? possiblyFoundRemoteAccount = Get(userId);
                if (possiblyFoundRemoteAccount != null)
                {
                    callback?.Invoke(possiblyFoundRemoteAccount);
                    yield break;
                }

                yield return new WaitForSecondsRealtime(1f);
            }

            callback?.Invoke("Timed out");
            yield break;
        }

        public IRemoteAccountProvider.RemoteAccount? Get(ulong userId)
        {
            for (int i = 0; i < UserDatas.Count; i++)
            {
                UserData userData = UserDatas[i];
                if (userData.NetworkID != userId) continue;
                if (string.IsNullOrWhiteSpace(userData.DisplayName))
                { break; }
                else
                { return new IRemoteAccountProvider.RemoteAccount(userData.DisplayName); }
            }

            if (NetworkManager.ServerClientId == userId)
            { return new IRemoteAccountProvider.RemoteAccount($"Server"); }
            else
            { return new IRemoteAccountProvider.RemoteAccount($"Client #{userId}"); }
        }

        public IEnumerator GetAsync(ulong userId, Action<TaskResult<IRemoteAccountProvider.RemoteAccount, string>> callback)
        {
            for (int i = 0; i < UserDatas.Count; i++)
            {
                UserData userData = UserDatas[i];
                if (userData.NetworkID != userId) continue;
                if (string.IsNullOrWhiteSpace(userData.DisplayName))
                { break; }
                else
                {
                    callback?.Invoke(new IRemoteAccountProvider.RemoteAccount(userData.DisplayName));
                    yield break;
                }
            }

            if (NetworkManager.ServerClientId == userId)
            {
                callback?.Invoke(new IRemoteAccountProvider.RemoteAccount($"Server"));
                yield break;
            }
            else
            {
                callback?.Invoke(new IRemoteAccountProvider.RemoteAccount($"Client #{userId}"));
                yield break;
            }
        }
    }
}

namespace Unity.Netcode
{
    public static class Services
    {
        static NetcodeServices? instance;

        public static NetcodeServices? Singleton
        {
            get => instance;
            set => instance = value;
        }
    }
}