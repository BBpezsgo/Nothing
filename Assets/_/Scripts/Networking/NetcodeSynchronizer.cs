using System;
using Authentication;
using Networking.Messages;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.Netcode;
using UnityEngine;

#nullable enable

namespace Networking
{
    [AddComponentMenu("Netcode/Synchronizer")]
    [RequireComponent(typeof(RuntimeNetStatsMonitor))]
    public class NetcodeSynchronizer : NetworkBehaviour
    {
        public static NetcodeSynchronizer? Instance;

        [SerializeField] internal bool Logs = true;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning($"[{nameof(NetcodeSynchronizer)}] Instance already registered, destroying this gameObject");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.F3))
            {
                RuntimeNetStatsMonitor comp = GetComponent<RuntimeNetStatsMonitor>();
                comp.Visible = !comp.Visible;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (Logs) Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Register events");

            if (NetworkManager.CustomMessagingManager != null)
            { NetworkManager.CustomMessagingManager.OnUnnamedMessage += OnReceivedUnnamedMessage; }
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager == null) return;

            if (Logs) Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Unregister events");

            if (NetworkManager.CustomMessagingManager != null)
            { NetworkManager.CustomMessagingManager.OnUnnamedMessage -= OnReceivedUnnamedMessage; }
        }

        void OnReceivedUnnamedMessage(ulong clientId, FastBufferReader reader)
        {
            if (Logs) Debug.Log($"[{nameof(NetcodeSynchronizer)}]: Received {reader.Length} bytes from client {clientId}");

            foreach (BaseMessage baseMessage in NetcodeMessaging.ReceiveUnnamedMessage(clientId, reader))
            {
                switch (baseMessage.Type)
                {
                    case MessageType.UserDataRequest:
                    {
                        UserDataRequestHeader message = (UserDataRequestHeader)baseMessage;

                        if (AuthManager.AuthProvider.IsAuthorized && AuthManager.AuthProvider.ID == message.ID)
                        {
                            NetcodeMessaging.SendUnnamedMessage(new UserDataHeader(new MessageHeader(MessageType.UserDataResponse, NetworkManager.LocalClientId))
                            {
                                UserName = AuthManager.AuthProvider.DisplayName ?? "null",
                                ID = AuthManager.AuthProvider.ID,
                            }, baseMessage.Sender);
                            break;
                        }

                        if (AuthManager.RemoteAccountProvider != null)
                        {
                            IRemoteAccountProvider.RemoteAccount? result = AuthManager.RemoteAccountProvider.Get(message.ID);
                            if (result != null)
                            {
                                NetcodeMessaging.SendUnnamedMessage(new UserDataHeader(new MessageHeader(MessageType.UserDataResponse, NetworkManager.LocalClientId))
                                {
                                    UserName = result.DisplayName ?? "null",
                                    ID = message.ID,
                                }, baseMessage.Sender);
                            }
                        }

                        break;
                    }

                    case MessageType.UserDataResponse:
                    {
                        UserDataHeader message = (UserDataHeader)baseMessage;
                        if (Services.Singleton != null)
                        { Services.Singleton.OnUserData(message); }
                        break;
                    }

                    case MessageType.UserDataRequestDirect:
                    {
                        if (AuthManager.AuthProvider.IsAuthorized)
                        {
                            NetcodeMessaging.SendUnnamedMessage(new UserDataHeader(new MessageHeader(MessageType.UserDataResponse, NetworkManager.LocalClientId))
                            {
                                UserName = AuthManager.AuthProvider.DisplayName ?? "null",
                                ID = AuthManager.AuthProvider.ID,
                            }, baseMessage.Sender);
                            break;
                        }

                        break;
                    }

                    case MessageType.Unknown:
                    default:
                        throw new Exception($"Unknown message type {baseMessage.Type} ({baseMessage.GetType()}) form client {baseMessage.Sender}");
                }
            }
        }
    }
}
