using UnityEngine;

namespace Game.Managers
{
    public class MenuStateProvider : MonoBehaviour
    {
        internal bool InNetcodeRoom
        {
            get
            {
                if (NetcodeUtils.IsOffline) return false;
                return
                    Unity.Netcode.NetworkManager.Singleton.IsListening && (
                        Unity.Netcode.NetworkManager.Singleton.IsServer || (
                            Unity.Netcode.NetworkManager.Singleton.IsClient &&
                            Unity.Netcode.NetworkManager.Singleton.IsConnectedClient
                        )
                    );
            }
        }

        public bool NetcodeIsLoading
        {
            get
            {
                if (NetcodeUtils.IsOffline) return false;
                return
                    Unity.Netcode.NetworkManager.Singleton.IsClient &&
                    !Unity.Netcode.NetworkManager.Singleton.IsConnectedClient;
            }
        }

        public bool Authorized => Authentication.AuthManager.IsAuthorized;
    }
}
