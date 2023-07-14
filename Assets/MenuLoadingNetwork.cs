using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using UnityEngine;
using UnityEngine.UIElements;

public class MenuLoadingNetwork : MonoBehaviour
{
    [SerializeField, ReadOnly] UIDocument UI;

    Label LabelStatus;

    void Awake()
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

    void FixedUpdate()
    {
        string result = "";

        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
        {
            result += $"Socket: {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}\n";
        }

        if (NetworkManager.Singleton.ShutdownInProgress)
        {
            result += $"Shutdown in progress\n";
        }

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
            { result += $"Connected as client\n"; }
        }

        LabelStatus.text = result;
    }
}
