using Unity.Netcode;

using UnityEngine;

namespace Networking
{
    public class OfflineManager : SingleInstance<OfflineManager>
    {
        public static bool IsOffline => NetworkManager.Singleton == null || instance.isOffline;

        [SerializeField] bool isOffline;
    }
}
