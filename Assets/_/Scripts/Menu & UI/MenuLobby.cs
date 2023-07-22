using Netcode.Transports.WebSocket;

using Networking;

using System;
using System.Collections.Generic;

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

            NetworkDiscovery.Instance.OnDiscoveredSomething += OnDiscoveredSomething;

            if (string.IsNullOrWhiteSpace(InputSocket.value))
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                { InputSocket.value = "ws://127.0.0.1:7777"; }
                else
                { InputSocket.value = "udp://127.0.0.1:7777"; }
            }

            RefreshDiscoveryUI();
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

            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
            {
                unityTransport.SetConnectionData(address, port, address);
                Debug.Log($"[{nameof(MenuLobby)}]: Start client on {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port} ...");
            }
            else
            { throw new NotImplementedException($"Unknown netcode transport {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType()}"); }

            bool success = NetworkManager.Singleton.StartClient();
            if (success)
            { Debug.Log($"[{nameof(MenuLobby)}]: Client started on {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}"); }
            else
            { Debug.LogError($"[{nameof(MenuLobby)}]: Failed to start client on {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}"); }

            NetworkDiscovery.Instance.StopDiscovery();
        }

        void RefreshDiscoveryUI()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                UI.rootVisualElement.Q<Button>("button-discovery-toggle").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

                UI.rootVisualElement.Q<Button>("button-discovery-refresh").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            }
            else if (NetworkDiscovery.Instance.IsRunning)
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
            if (!NetworkManager.Singleton.IsListening &&
                NetworkDiscovery.Instance.IsRunning &&
                NetworkDiscovery.IsSupported)
            {
                NetworkDiscovery.DiscoveredServers.Clear();
                NetworkDiscovery.Instance.ClientBroadcast(new DiscoveryBroadcastData());
            }
            RefreshDiscoveryUI();
        }

        bool ComputeSocketInput(string input, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Input is empty";
                return false;
            }

            input = input.Trim();

            if (!Uri.TryCreate(input, UriKind.Absolute, out Uri uri))
            {
                error = "Failed to parse";
                return false;
            }

            GameObject obj = NetworkManager.Singleton.gameObject;

            if (uri.Scheme == "udp")
            {
                if (!obj.TryGetComponent(out UnityTransport unityTransport))
                {
                    error = $"UDP not supported :(";
                    return false;
                }

                if (!System.Net.IPAddress.TryParse(uri.Host ?? "", out System.Net.IPAddress address))
                {
                    error = $"Invalid IP Address \"{uri.Host}\"";
                    return false;
                }

                if (address.ToString() != uri.Host)
                {
                    error = $"Invalid IP Address \"{uri.Host}\"";
                    return false;
                }

                if (uri.IsDefaultPort)
                {
                    error = $"No port specified";
                    return false;
                }

                if (uri.Port < 1 || uri.Port > ushort.MaxValue)
                {
                    error = $"Invalid port {uri.Port}";
                    return false;
                }

                NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
                Debug.Log($"[{nameof(MenuLobby)}]: {nameof(NetworkManager.Singleton.NetworkConfig.NetworkTransport)} set to {nameof(UnityTransport)}");

                unityTransport.SetConnectionData(address.ToString(), (ushort)uri.Port, address.ToString());
                Debug.Log($"[{nameof(MenuLobby)}]: Connection data set to {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}");
                return true;
            }

            if (uri.Scheme == "ws" || uri.Scheme == "wss")
            {
                if (!obj.TryGetComponent(out WebSocketTransport webSocketTransport))
                {
                    error = $"WebSocket not supported :(";
                    return false;
                }

                if (uri.IsDefaultPort)
                {
                    error = $"No port specified";
                    return false;
                }

                if (uri.Port < 1 || uri.Port > ushort.MaxValue)
                {
                    error = $"Invalid port {uri.Port}";
                    return false;
                }

                NetworkManager.Singleton.NetworkConfig.NetworkTransport = webSocketTransport;
                Debug.Log($"[{nameof(MenuLobby)}]: {nameof(NetworkManager.Singleton.NetworkConfig.NetworkTransport)} set to {nameof(WebSocketTransport)}");

                webSocketTransport.AllowForwardedRequest = false;
                webSocketTransport.CertificateBase64String = "";
                webSocketTransport.ConnectAddress = uri.Host;
                webSocketTransport.Port = (ushort)uri.Port;
                webSocketTransport.SecureConnection = (uri.Scheme == "wss");
                webSocketTransport.Path = uri.AbsolutePath;

                Debug.Log($"[{nameof(MenuLobby)}]: Connection data set to {(webSocketTransport.SecureConnection ? "wss" : "ws")}://{webSocketTransport.ConnectAddress}:{webSocketTransport.Port}{webSocketTransport.Path}");
                return true;
            }

            error = $"Unknown scheme \"{uri.Scheme}\"";
            return false;
        }

        string GetSocket()
        {
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
            { return $"{unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}"; }
            else if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is WebSocketTransport webSocketTransport)
            { return $"{(webSocketTransport.SecureConnection ? "wss" : "ws")}://{webSocketTransport.ConnectAddress}:{webSocketTransport.Port}{webSocketTransport.Path}"; }
            else
            { throw new NotImplementedException($"Unknown netcode transport {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType()}"); }
        }

        void ButtonHost()
        {
            if (!ComputeSocketInput(InputSocket.value, out string socketError))
            {
                LabelSocketError.text = socketError;
                return;
            }


            Debug.Log($"[{nameof(MenuLobby)}]: Start server on {GetSocket()} ...");
            bool success = NetworkManager.Singleton.StartServer();
            if (success)
            {
                Debug.Log($"[{nameof(MenuLobby)}]: Server started on {GetSocket()}");
            }
            else
            { Debug.LogError($"[{nameof(MenuLobby)}]: Failed to start server on {GetSocket()}"); }

            NetworkDiscovery.Instance.StopDiscovery();
        }

        void ButtonConnect()
        {
            if (!ComputeSocketInput(InputSocket.value, out string socketError))
            {
                LabelSocketError.text = socketError;
                return;
            }

            { Debug.Log($"[{nameof(MenuLobby)}]: Start client on {GetSocket()} ..."); }
            bool success = NetworkManager.Singleton.StartClient();
            if (success)
            { Debug.Log($"[{nameof(MenuLobby)}]: Client started on {GetSocket()}"); }
            else
            { Debug.LogError($"[{nameof(MenuLobby)}]: Failed to start client on {GetSocket()}"); }

            NetworkDiscovery.Instance.StopDiscovery();
        }
    }
}
