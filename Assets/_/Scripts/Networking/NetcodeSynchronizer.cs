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
                var comp = GetComponent<RuntimeNetStatsMonitor>();
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

            var messages = NetcodeMessaging.ReceiveUnnamedMessage(clientId, reader);

            for (int i = 0; i < messages.Length; i++)
            {
                BaseMessage baseMessage = messages[i];

                switch (baseMessage.Type)
                {
                    case MessageType.USER_DATA_REQUEST:
                        {
                            UserDataRequestHeader message = (UserDataRequestHeader)baseMessage;

                            if (AuthManager.AuthProvider.IsAuthorized && AuthManager.AuthProvider.ID == message.ID)
                            {
                                NetcodeMessaging.SendUnnamedMessage(new UserDataHeader(new MessageHeader(MessageType.USER_DATA, NetworkManager.LocalClientId))
                                {
                                    UserName = AuthManager.AuthProvider.DisplayName ?? "null",
                                    ID = AuthManager.AuthProvider.ID,
                                }, baseMessage.Sender);
                                break;
                            }

                            if (AuthManager.RemoteAccountProvider != null)
                            {
                                var result = AuthManager.RemoteAccountProvider.Get(message.ID);
                                if (result != null)
                                {
                                    NetcodeMessaging.SendUnnamedMessage(new UserDataHeader(new MessageHeader(MessageType.USER_DATA, NetworkManager.LocalClientId))
                                    {
                                        UserName = result.DisplayName ?? "null",
                                        ID = message.ID,
                                    }, baseMessage.Sender);
                                }
                            }

                            break;
                        }

                    case MessageType.USER_DATA:
                        {
                            UserDataHeader message = (UserDataHeader)baseMessage;
                            if (Services.Singleton != null)
                            { Services.Singleton.OnUserData(message); }
                            break;
                        }

                    case MessageType.USER_DATA_REQUEST_DIRECT:
                        {
                            if (AuthManager.AuthProvider.IsAuthorized)
                            {
                                NetcodeMessaging.SendUnnamedMessage(new UserDataHeader(new MessageHeader(MessageType.USER_DATA, NetworkManager.LocalClientId))
                                {
                                    UserName = AuthManager.AuthProvider.DisplayName ?? "null",
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
    }
}
