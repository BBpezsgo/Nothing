using System.Collections.Generic;
using System.Linq;
using System.Net;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using UnityEngine;

namespace Networking
{
    public class NetworkDiscovery : NetworkDiscovery<DiscoveryBroadcastData, NetworkDiscovery.DiscoveryResponseData>
    {
        public struct DiscoveryResponseData : INetworkSerializable
        {
            public ushort Port;
            public string Address;
            public string ServerName;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Port);
                serializer.SerializeValue(ref Address);
                serializer.SerializeValue(ref ServerName);
            }
        }

        public delegate void OnDiscoveredSomethingEvent();

        static NetworkDiscovery instance;

        [SerializeField, ReadOnly] NetworkManager NetworkManager;

        [SerializeField, ReadOnly] bool HasStartedWithServer = false;

        [SerializeField] string serverName = "EnterName";

        readonly Dictionary<IPAddress, NetworkDiscovery.DiscoveryResponseData> discoveredServers = new();

        public OnDiscoveredSomethingEvent OnDiscoveredSomething;

        public static NetworkDiscovery Instance => instance;

        public static IDictionary<IPAddress, NetworkDiscovery.DiscoveryResponseData> DiscoveredServers
        {
            get
            {
                if (instance == null) return new Dictionary<IPAddress, NetworkDiscovery.DiscoveryResponseData>();
                return instance.discoveredServers;
            }
        }

        public static string ServerName
        {
            get
            {
                if (instance == null) return null;
                return instance.serverName;
            }
            set
            {
                if (instance == null) return;
                instance.serverName = value;
            }
        }

        void Awake()
        {
            if (instance != null)
            {
                Debug.LogWarning($"[{nameof(NetworkDiscovery)}]: Instance already registered, destroying self");
                Object.Destroy(this);
                return;
            }

            instance = this;

            if (!TryGetComponent(out NetworkManager))
            { Debug.LogError($"[{nameof(NetworkDiscovery)}]: {nameof(NetworkManager)} is null", this); }

            if (!IsSupported)
            {
                Debug.Log($"[{nameof(NetworkDiscovery)}]: Not supported");
            }
        }

        public static bool IsSupported =>
            Application.isEditor ||
            SupportedPlatforms.Contains(Application.platform);

        static readonly RuntimePlatform[] SupportedPlatforms = new RuntimePlatform[]
        {
            RuntimePlatform.Android,
            RuntimePlatform.WindowsEditor,
            RuntimePlatform.WindowsPlayer,
            RuntimePlatform.WindowsServer,
            RuntimePlatform.LinuxEditor,
            RuntimePlatform.LinuxPlayer,
            RuntimePlatform.LinuxServer,
            RuntimePlatform.IPhonePlayer,
            RuntimePlatform.EmbeddedLinuxArm32,
            RuntimePlatform.EmbeddedLinuxArm64,
            RuntimePlatform.EmbeddedLinuxX64,
            RuntimePlatform.EmbeddedLinuxX86,
        };

        void Update()
        {
            if (!IsSupported) return;

            if (!HasStartedWithServer &&
                !IsRunning &&
                NetworkManager.IsServer)
            {
                StartServer();
                HasStartedWithServer = true;
            }
            else if (HasStartedWithServer &&
                IsRunning &&
                !NetworkManager.IsListening)
            {
                StopDiscovery();
                HasStartedWithServer = false;
            }
        }

        protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out NetworkDiscovery.DiscoveryResponseData response)
        {
            response = new NetworkDiscovery.DiscoveryResponseData()
            {
                ServerName = serverName,
                Address = ((UnityTransport)NetworkManager.NetworkConfig.NetworkTransport).ConnectionData.ServerListenAddress,
                Port = ((UnityTransport)NetworkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
            };
            return true;
        }

        protected override void ResponseReceived(IPEndPoint sender, NetworkDiscovery.DiscoveryResponseData response)
        {
            discoveredServers.AddOrModify(sender.Address, response);
            OnDiscoveredSomething?.Invoke();
        }
    }
}
