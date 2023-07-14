using UnityEngine;

public class MenuStateProvider : MonoBehaviour
{
    // internal bool InLobby => Photon.Pun.PhotonNetwork.InLobby;

    internal bool InNetcodeRoom
    {
        get
        {
            if (Unity.Netcode.NetworkManager.Singleton == null) return false;
            return
                Unity.Netcode.NetworkManager.Singleton.IsListening && (
                    Unity.Netcode.NetworkManager.Singleton.IsServer || (
                        Unity.Netcode.NetworkManager.Singleton.IsClient &&
                        Unity.Netcode.NetworkManager.Singleton.IsConnectedClient
                    )
                );
        }
    }

    // internal bool InPhotonRoom => Photon.Pun.PhotonNetwork.InRoom;

    public bool NetcodeIsLoading
    {
        get
        {
            if (Unity.Netcode.NetworkManager.Singleton == null) return false;
            return
                Unity.Netcode.NetworkManager.Singleton.IsClient &&
                !Unity.Netcode.NetworkManager.Singleton.IsConnectedClient;
        }
    }

    public bool IsOffline => Unity.Netcode.NetworkManager.Singleton == null;

    public bool Authorized => AuthManager.IsAuthorized;
}
