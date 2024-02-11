using Netcode.Transports.WebSocket;

using Networking;

using System.Collections.Generic;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class MenuPause : MonoBehaviour
    {
        [SerializeField, ReadOnly] UIDocument UI;

        [SerializeField] VisualTreeAsset PanelPlayer;

        Label LabelRoomName;
        ScrollView PlayersScrollView;

        [SerializeField, ReadOnly] float UpdatePlayersTimer;

        void Awake()
        {
            UI = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            LabelRoomName = UI.rootVisualElement.Q<Label>("label-room-name");
            UI.rootVisualElement.Q<Button>("button-disconnect").clicked += ButtonDisconnect;
            PlayersScrollView = UI.rootVisualElement.Q<ScrollView>("players");

            if (NetcodeUtils.IsActiveOffline)
            {
                UI.rootVisualElement.Q<VisualElement>("players-container").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                // LabelRoomName.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                LabelRoomName.text = "Offline";

                UI.rootVisualElement.Q<Button>("button-disconnect").text = "Exit";
            }
            else
            {
                UI.rootVisualElement.Q<VisualElement>("players-container").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                // LabelRoomName.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);

                if (NetworkManager.Singleton.IsServer)
                { UI.rootVisualElement.Q<Button>("button-disconnect").text = "Stop Server"; }
                else
                { UI.rootVisualElement.Q<Button>("button-disconnect").text = "Disconnect"; }

                UpdatePlayersTimer = 2f;
                RefreshPlayerList();
            }
        }

        internal void RefreshPlayerList()
        {
            PlayersScrollView.contentContainer.Clear();
            NetworkManager net = NetworkManager.Singleton;
            Dictionary<ulong, NetcodeServices.Client> clientsS = FindObjectOfType<NetcodeServices>().Clients;

            foreach (KeyValuePair<ulong, NetcodeServices.Client> client in clientsS)
            {
                TemplateContainer newPanel = PanelPlayer.Instantiate();
                if (client.Value.NetworkID == NetworkManager.ServerClientId)
                { newPanel.Q<Label>("label-name").text = $"Server"; }
                else
                { newPanel.Q<Label>("label-name").text = $"Client {client.Value.NetworkID}"; }

                if (net.NetworkConfig.NetworkTransport is UnityTransport transport)
                { newPanel.Q<Label>("label-ping").text = $"{transport.GetCurrentRtt(client.Value.NetworkID)} ms"; }
                else
                { newPanel.Q<Label>("label-ping").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None); }

                newPanel.Q<Button>("button-kick").style.display = DisplayStyle.None;

                PlayersScrollView.Add(newPanel);
            }

            if (net.IsServer)
            {
                IReadOnlyDictionary<ulong, NetworkClient> clients = net.ConnectedClients;
                foreach (KeyValuePair<ulong, NetworkClient> client in clients)
                {
                    if (clientsS.ContainsKey(client.Key)) continue;
                    TemplateContainer newPanel = PanelPlayer.Instantiate();
                    if (client.Value.ClientId == NetworkManager.ServerClientId)
                    { newPanel.Q<Label>("label-name").text = $"Server"; }
                    else
                    { newPanel.Q<Label>("label-name").text = $"Client {client.Value.ClientId}"; }

                    if (net.NetworkConfig.NetworkTransport is UnityTransport transport)
                    { newPanel.Q<Label>("label-ping").text = $"{transport.GetCurrentRtt(client.Value.ClientId)} ms"; }
                    else
                    { newPanel.Q<Label>("label-ping").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None); }

                    newPanel.Q<Button>("button-kick").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

                    PlayersScrollView.Add(newPanel);
                }

                if (!clientsS.ContainsKey(NetworkManager.ServerClientId))
                {
                    TemplateContainer newPanel = PanelPlayer.Instantiate();
                    newPanel.Q<Label>("label-name").text = $"Server";

                    if (net.NetworkConfig.NetworkTransport is UnityTransport transport)
                    { newPanel.Q<Label>("label-ping").text = $"{transport.GetCurrentRtt(NetworkManager.ServerClientId)} ms"; }
                    else
                    { newPanel.Q<Label>("label-ping").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None); }

                    Button buttonKick = newPanel.Q<Button>("button-kick");
                    buttonKick.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

                    PlayersScrollView.Add(newPanel);
                }
            }
        }

        void ButtonDisconnect()
        {
            Debug.Log($"[{nameof(MenuRoom)}]: Shutting down ...", this);
            NetworkManager.Singleton.Shutdown();
            Debug.Log($"[{nameof(MenuRoom)}]: Shut down", this);
            MenuNavigator.Instance.IsPaused = false;
        }

        void Update()
        {
            if (!NetcodeUtils.IsActiveOffline)
            {
                if (!string.IsNullOrWhiteSpace(NetworkManager.Singleton.ConnectedHostname))
                { LabelRoomName.text = NetworkManager.Singleton.ConnectedHostname; }
                else if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
                { LabelRoomName.text = $"{unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}"; }
                else if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is WebSocketTransport webSocketTransport)
                { LabelRoomName.text = $"{(webSocketTransport.SecureConnection ? "wss" : "ws")}://{webSocketTransport.ConnectAddress}:{webSocketTransport.Port}{webSocketTransport.Path}"; }
                else
                { LabelRoomName.text = "?"; }

                UpdatePlayersTimer -= Time.deltaTime;
                if (UpdatePlayersTimer <= 0f)
                {
                    UpdatePlayersTimer = 2f;
                    RefreshPlayerList();
                }
            }
        }
    }
}
