using Unity.Netcode;
using UnityEngine;

namespace Game.Managers
{
    public class MenuStateProvider : MonoBehaviour
    {
        internal bool InNetcodeRoom
        {
            get
            {
                NetworkManager net = NetworkManager.Singleton;

                if (net == null) return false;

                if (!net.IsListening) return false;

                if (net.IsServer) return true;

                if (net.IsConnectedClient) return true;

                return false;
            }
        }

        public bool NetcodeIsLoading
        {
            get
            {
                NetworkManager net = NetworkManager.Singleton;

                if (net == null) return false;

                if (net.ShutdownInProgress) return true;

                if (net.IsClient && !net.IsConnectedClient) return true;

                return false;
            }
        }

        public bool Authorized => Authentication.AuthManager.IsAuthorized;
    }
}
