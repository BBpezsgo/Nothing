using Netcode.Transports.WebSocket;

using Networking;

using System.Collections.Generic;

using Unity.Netcode;

using Unity.Netcode.Transports.UTP;

using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class MenuRoom : MonoBehaviour
    {
        [SerializeField, ReadOnly] UIDocument UI;

        [SerializeField] VisualTreeAsset PanelPlayer;

        Label LabelRoomName;
        ScrollView PlayersScrollView;

        [SerializeField, ReadOnly] float UpdatePlayersTimer;

        void Start()
        {
            UI = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            LabelRoomName = UI.rootVisualElement.Q<Label>("label-room-name");
            PlayersScrollView = UI.rootVisualElement.Q<ScrollView>("players");

            UpdatePlayersTimer = 2f;
            RefreshPlayerList();
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

                Button buttonKick = newPanel.Q<Button>("button-kick");
                if (!net.IsServer)
                {
                    buttonKick.style.display = DisplayStyle.None;
                }
                else
                {
                    buttonKick.userData = client.Value.NetworkID.ToString();
                    buttonKick.clickable.clickedWithEventInfo += Clickable_clickedWithEventInfo;
                }

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

                    Button buttonKick = newPanel.Q<Button>("button-kick");
                    buttonKick.userData = client.Value.ClientId.ToString();
                    buttonKick.clickable.clickedWithEventInfo += Clickable_clickedWithEventInfo;

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

        void Clickable_clickedWithEventInfo(EventBase obj)
        {
            var clientId = ulong.Parse(((VisualElement)obj.target).userData.ToString());
            Debug.Log($"[{nameof(MenuRoom)}]: Kick client {clientId}");
            NetworkManager.Singleton.DisconnectClient(clientId);
        }

        void FixedUpdate()
        {
            if (!string.IsNullOrWhiteSpace(NetworkManager.Singleton.ConnectedHostname))
            { LabelRoomName.text = NetworkManager.Singleton.ConnectedHostname; }
            else if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
            { LabelRoomName.text = $"{unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}"; }
            else if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is WebSocketTransport webSocketTransport)
            { LabelRoomName.text = $"{(webSocketTransport.SecureConnection ? "wss" : "ws")}://{webSocketTransport.ConnectAddress}:{webSocketTransport.Port}{webSocketTransport.Path}"; }
            else
            { LabelRoomName.text = "?"; }

            UpdatePlayersTimer -= Time.fixedDeltaTime;
            if (UpdatePlayersTimer <= 0f)
            {
                UpdatePlayersTimer = 2f;
                RefreshPlayerList();
            }
        }
    }
}
