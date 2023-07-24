using Unity.Netcode;

using UnityEngine;

namespace Networking.Managers
{
    public class NetcodeManager : SingleInstance<NetcodeManager>
    {
        void Start()
        {
            NetcodeVariableSerializers.Init();
        }

        void Singleton_OnServerStopped(bool isHost)
        {
            if (isHost)
            {
                Debug.Log($"[{nameof(NetcodeManager)}]: Host stopped");
            }
            else
            {
                Debug.Log($"[{nameof(NetcodeManager)}]: Server stopped");
            }
        }

        void Singleton_OnServerStarted()
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Server started");
        }

        void Singleton_OnClientStopped(bool isHost)
        {
            if (isHost)
            {
                Debug.Log($"[{nameof(NetcodeManager)}]: Host stopped");
            }
            else
            {
                if (string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
                {
                    Debug.Log($"[{nameof(NetcodeManager)}]: Client stopped without any reason");
                }
                else
                {
                    Debug.Log($"[{nameof(NetcodeManager)}]: Client stopped, reason: \"{NetworkManager.Singleton.DisconnectReason}\"");
                }
            }
        }

        void Singleton_OnClientStarted()
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Client started");
        }

        void Singleton_OnClientDisconnectCallback(ulong clientId)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Client {clientId} disconnected");
            var v = FindObjectOfType<Game.UI.MenuRoom>();
            if (v != null) v.RefreshPlayerList();
        }

        void Singleton_OnClientConnectedCallback(ulong clientId)
        {
            Debug.Log($"[{nameof(NetcodeManager)}]: Client {clientId} connected");
            var v = FindObjectOfType<Game.UI.MenuRoom>();
            if (v != null) v.RefreshPlayerList();
        }
    }
}

namespace Networking
{
    static class NetcodeVariableSerializers
    {
        public static void Init()
        {
            UserNetworkVariableSerialization<ulong[]>.WriteValue = Write;
            UserNetworkVariableSerialization<ulong[]>.ReadValue = Read;
        }

        static void Write(FastBufferWriter writer, in ulong[] v)
        {
            if (v is null)
            { throw new System.NullReferenceException($"{nameof(v)} is null"); }
            writer.WriteValue((byte)v.Length);
            for (int i = 0; i < v.Length; i++)
            { writer.WriteValue(v[i]); }
        }

        static void Read(FastBufferReader reader, out ulong[] v)
        {
            reader.ReadValue(out byte length);
            v = new ulong[length];
            for (int i = 0; i < v.Length; i++)
            {
                reader.ReadValue(out ulong item);
                v[i] = item;
            }
        }
    }
}