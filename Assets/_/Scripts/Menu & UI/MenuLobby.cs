using System;
using Netcode.Transports.Offline;
using Networking;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class MenuLobby : MonoBehaviour
    {
        [SerializeField, ReadOnly] UIDocument UI;

        [SerializeField] VisualTreeAsset DiscoveredServerElement;

        ScrollView ScrollviewDiscoveredServers;

        TextField InputSocket;
        Label LabelSocketError;

        void Awake()
        {
            UI = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            InputSocket = UI.rootVisualElement.Q<TextField>("input-socket");
            LabelSocketError = UI.rootVisualElement.Q<Label>("label-socket-error");
            UI.rootVisualElement.Q<Button>("button-offline").clicked += ButtonOffline;
            UI.rootVisualElement.Q<Button>("button-host").clicked += ButtonHost;
            UI.rootVisualElement.Q<Button>("button-connect").clicked += ButtonConnect;
#if UNITY_EDITOR
            UI.rootVisualElement.Q<Button>("button-quit").clicked += () => UnityEditor.EditorApplication.isPlaying = false;
#else
            UI.rootVisualElement.Q<Button>("button-quit").clicked += Application.Quit;
#endif
            ScrollviewDiscoveredServers = UI.rootVisualElement.Q<ScrollView>("rooms");
            ScrollviewDiscoveredServers.contentContainer.Clear();

            UI.rootVisualElement.Q<Button>("button-discovery-toggle").clicked += ButtonDiscoveryToggle;
            UI.rootVisualElement.Q<Button>("button-discovery-refresh").clicked += ButtonDiscoveryRefresh;

            if (NetworkDiscovery.Instance != null)
            { NetworkDiscovery.Instance.OnDiscoveredSomething += OnDiscoveredSomething; }

            if (string.IsNullOrWhiteSpace(InputSocket.value))
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                { InputSocket.value = "ws://127.0.0.1:7777"; }
                else
                { InputSocket.value = "udp://127.0.0.1:7777"; }
            }

            RefreshDiscoveryUI();
        }

        private void ButtonOffline()
        {
            if (!NetworkManager.Singleton.gameObject.TryGetComponent(out OfflineTransport transport))
            {
                LabelSocketError.text = "Offline mode not supported :(";
                return;
            }

            NetworkManager.Singleton.NetworkConfig.NetworkTransport = transport;
            Debug.Log($"[{nameof(MenuLobby)}]: {nameof(NetworkManager.Singleton.NetworkConfig.NetworkTransport)} set to {nameof(OfflineTransport)}", this);

            { Debug.Log($"[{nameof(MenuLobby)}]: Start server on {NetcodeUtils.NetworkConfig} ...", this); }
            bool success = NetworkManager.Singleton.StartServer();
            if (success)
            { Debug.Log($"[{nameof(MenuLobby)}]: Server started on {NetcodeUtils.NetworkConfig}", this); }
            else
            { Debug.LogError($"[{nameof(MenuLobby)}]: Failed to start server on {NetcodeUtils.NetworkConfig}", this); }

            NetworkDiscovery.Instance.StopDiscovery();
        }

        void OnDisable()
        {
            NetworkDiscovery.Instance.OnDiscoveredSomething -= OnDiscoveredSomething;
        }

        void OnDiscoveredSomething()
        {
            ScrollviewDiscoveredServers.contentContainer.Clear();

            foreach (var discoveredServer in NetworkDiscovery.DiscoveredServers.Values)
            {
                TemplateContainer newElement = DiscoveredServerElement.Instantiate();

                newElement.Q<Label>("label-name").text = $"{discoveredServer.Address}:{discoveredServer.Port}";

                newElement.Q<Button>().name = $"server-{discoveredServer.Address}:{discoveredServer.Port}";
                newElement.Q<Button>().clickable.clickedWithEventInfo += ButtonConnectDiscoveredServer;

                ScrollviewDiscoveredServers.Add(newElement);
            }
        }

        void ButtonConnectDiscoveredServer(EventBase e)
        {
            string socket = ((VisualElement)e.target).name.Split('-')[1];
            ushort port = ushort.Parse(socket.Split(':')[^1]);
            string address = socket[..(socket.Length - socket.Split(':')[^1].Length - 1)];

            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is not UnityTransport unityTransport)
            {
                unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
                // throw new NotImplementedException($"Unknown netcode transport {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType()}");
            }

            unityTransport.SetConnectionData(address, port, address);
            Debug.Log($"[{nameof(MenuLobby)}]: Start client on {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port} ...", this);

            bool success = NetworkManager.Singleton.StartClient();
            if (success)
            { Debug.Log($"[{nameof(MenuLobby)}]: Client started on {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}", this); }
            else
            { Debug.LogError($"[{nameof(MenuLobby)}]: Failed to start client on {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}", this); }

            NetworkDiscovery.Instance.StopDiscovery();
        }

        void RefreshDiscoveryUI()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                UI.rootVisualElement.Q<Button>("button-discovery-toggle").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

                UI.rootVisualElement.Q<Button>("button-discovery-refresh").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            }
            else if (NetworkDiscovery.Instance != null && NetworkDiscovery.Instance.IsRunning)
            {
                UI.rootVisualElement.Q<Button>("button-discovery-toggle").text = "Stop Discovery";
                UI.rootVisualElement.Q<Button>("button-discovery-toggle").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);

                UI.rootVisualElement.Q<Button>("button-discovery-refresh").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            }
            else
            {
                UI.rootVisualElement.Q<Button>("button-discovery-toggle").text = "Start Discovery";
                UI.rootVisualElement.Q<Button>("button-discovery-toggle").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);

                UI.rootVisualElement.Q<Button>("button-discovery-refresh").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            }
        }

        void ButtonDiscoveryToggle()
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                if (NetworkDiscovery.Instance.IsRunning)
                {
                    NetworkDiscovery.Instance.StopDiscovery();
                }
                else if (NetworkDiscovery.IsSupported)
                {
                    NetworkDiscovery.Instance.StartClient();
                    NetworkDiscovery.Instance.ClientBroadcast(new DiscoveryBroadcastData());
                }
            }
            RefreshDiscoveryUI();
        }

        void ButtonDiscoveryRefresh()
        {
            NetworkDiscovery.Instance.OnDiscoveredSomething -= OnDiscoveredSomething;
            NetworkDiscovery.Instance.OnDiscoveredSomething += OnDiscoveredSomething;

            if (!NetworkManager.Singleton.IsListening &&
                NetworkDiscovery.Instance.IsRunning &&
                NetworkDiscovery.IsSupported)
            {
                NetworkDiscovery.DiscoveredServers.Clear();
                NetworkDiscovery.Instance.ClientBroadcast(new DiscoveryBroadcastData());
            }
            RefreshDiscoveryUI();
        }

        void ButtonHost()
        {
            LabelSocketError.text = string.Empty;
            NetworkDiscovery.Instance.StopDiscovery();
            StartCoroutine(NetcodeUtils.HostAsync(InputSocket.value, error => { if (error != null) LabelSocketError.text = error; }, this));
        }

        void ButtonConnect()
        {
            LabelSocketError.text = string.Empty;
            NetworkDiscovery.Instance.StopDiscovery();
            StartCoroutine(NetcodeUtils.ConnectAsync(InputSocket.value, error => { if (error != null) LabelSocketError.text = error; }, this));
        }
    }
}
