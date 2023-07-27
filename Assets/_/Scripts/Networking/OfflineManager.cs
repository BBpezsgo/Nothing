using Unity.Netcode;

using UnityEngine;

namespace Networking
{
    public class OfflineManager : SingleInstance<OfflineManager>
    {
        public static bool IsOffline
        {
            get => NetworkManager.Singleton == null || instance.isOffline;
            set => instance.isOffline = value;
        }

        [SerializeField] bool isOffline;
    }
}
