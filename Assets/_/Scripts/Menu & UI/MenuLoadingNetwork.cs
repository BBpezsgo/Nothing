using Netcode.Transports.Offline;
using Netcode.Transports.WebSocket;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class MenuLoadingNetwork : MonoBehaviour
    {
        [SerializeField, ReadOnly] UIDocument UI;

        Label LabelStatus;

        void Start()
        {
            UI = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            LabelStatus = UI.rootVisualElement.Q<Label>("label-status");
            UI.rootVisualElement.Q<Button>("button-cancel").clicked += OnButtonCancel;
        }

        void OnButtonCancel()
        {
            NetworkManager.Singleton.Shutdown();
        }

        void Update()
        {
            string result = "";

            NetworkTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;

            if (transport is UnityTransport unityTransport)
            { result += $"Socket: {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}\n"; }
            else if (transport is WebSocketTransport webSocketTransport)
            { result += $"Socket: {(webSocketTransport.SecureConnection ? "wss" : "ws")}://{webSocketTransport.ConnectAddress}:{webSocketTransport.Port}{webSocketTransport.Path}\n"; }
            else if (transport is OfflineTransport)
            { result += $"Socket: none\n"; }

            if (NetworkManager.Singleton.ShutdownInProgress)
            { result += $"Shutdown in progress\n"; }

            if (!NetworkManager.Singleton.IsListening)
            { result += $"Not listening\n"; }

            if (NetworkManager.Singleton.IsHost)
            { result += $"Host is running\n"; }

            if (NetworkManager.Singleton.IsServer)
            { result += $"Server is running\n"; }

            if (NetworkManager.Singleton.IsClient)
            {
                if (!NetworkManager.Singleton.IsConnectedClient)
                { result += $"Connecting ...\n"; }

                if (!NetworkManager.Singleton.IsApproved)
                { result += $"Awaiting approval ...\n"; }

                if (NetworkManager.Singleton.IsConnectedClient)
                { result += $"Connected\n"; }
            }

            LabelStatus.text = result;
        }
    }
}
