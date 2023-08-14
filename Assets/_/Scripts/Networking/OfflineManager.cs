using Netcode.Transports.Offline;
using Unity.Netcode;
using UnityEngine;

namespace Networking
{
    public class OfflineManager : SingleInstance<OfflineManager>
    {
        public static bool IsActiveOffline
        {
            get
            {
                if (NetworkManager.Singleton == null) return false;

                if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is not OfflineTransport) return false;

                return NetworkManager.Singleton.IsListening;
            }
        }

#if UNITY_EDITOR
        [SerializeField] bool StartOffline;
        [SerializeField] string LoadScene;

        void Start()
        {
            if (!StartOffline) return;

            if (!NetworkManager.Singleton.gameObject.TryGetComponent(out OfflineTransport transport))
            { return; }

            NetworkManager.Singleton.NetworkConfig.NetworkTransport = transport;

            bool success = NetworkManager.Singleton.StartServer();
            if (!success)
            {
                Debug.LogError($"[{nameof(OfflineManager)}]: Failed to start offline server", this);
                return;
            }

            NetworkDiscovery.Instance.StopDiscovery();

            if (LoadScene == null) return;

            SceneManager.LoadScene(LoadScene);
        }
#endif
    }
}
