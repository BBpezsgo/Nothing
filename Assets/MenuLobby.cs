using System;
using System.Collections.Generic;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using UnityEngine;
using UnityEngine.UIElements;

public class MenuLobby : MonoBehaviour
{
    [SerializeField, ReadOnly] UIDocument UI;

    [SerializeField] VisualTreeAsset DiscoveredServerElement;

    ScrollView ScrollviewDiscoveredServers;

    TextField InputAddress;
    TextField InputPort;

    void Awake()
    {
        UI = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        InputAddress = UI.rootVisualElement.Q<TextField>("input-address");
        InputPort = UI.rootVisualElement.Q<TextField>("input-port");
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

    void ButtonConnectDiscoveredServer(EventBase @base)
    {
        string socket = ((VisualElement)@base.target).name.Split('-')[1];
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
            else
            {
                NetworkDiscovery.Instance.StartClient();
                NetworkDiscovery.Instance.ClientBroadcast(new DiscoveryBroadcastData());
            }
        }
        RefreshDiscoveryUI();
    }

    void ButtonDiscoveryRefresh()
    {
        if (!NetworkManager.Singleton.IsListening)
        {
            if (NetworkDiscovery.Instance.IsRunning)
            {
                NetworkDiscovery.DiscoveredServers.Clear();
                NetworkDiscovery.Instance.ClientBroadcast(new DiscoveryBroadcastData());
            }
        }
        RefreshDiscoveryUI();
    }

    void ButtonHost()
    {
        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
        {
            unityTransport.SetConnectionData(InputAddress.value, ushort.Parse(InputPort.value), InputAddress.value);
            Debug.Log($"[{nameof(MenuLobby)}]: Start server on {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port} ...");
        }
        else
        { throw new NotImplementedException($"Unknown netcode transport {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType()}"); }

        bool success = NetworkManager.Singleton.StartServer();
        if (success)
        {
            if (!UnityEngine.SceneManagement.SceneManager.GetSceneByName("GameScene").isLoaded)
            {
                Debug.Log($"[{nameof(MenuLobby)}]: Scene \"GameScene\" not loaded on server, so loading ...");
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }

            Debug.Log($"[{nameof(MenuLobby)}]: Server started on {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}");
        }
        else
        { Debug.LogError($"[{nameof(MenuLobby)}]: Failed to start server on {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}"); }

        NetworkDiscovery.Instance.StopDiscovery();
    }

    void ButtonConnect()
    {
        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
        {
            unityTransport.SetConnectionData(InputAddress.value, ushort.Parse(InputPort.value), InputAddress.value);
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
}
