using System;
using System.Collections.Generic;
using Game;
using Game.Managers;
using Unity.Netcode;
using UnityEngine;

public struct ClientObject : INetworkSerializable, IEquatable<ClientObject>
{
    public ulong ClientId;
    public ulong ObjectId;

    public ClientObject(ulong clientId, ulong objectId)
    {
        ClientId = clientId;
        ObjectId = objectId;
    }

    public readonly bool Equals(ClientObject other) =>
        ClientId == other.ClientId &&
        ObjectId == other.ObjectId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref ObjectId);
    }
}

public class GameManager : SingleNetworkInstance<GameManager>
{
    NetworkList<ClientObject> ClientObjects;
    [SerializeField] GameObject ClientObjectPrefab;
    [SerializeField] List<GameObject> ClientObjectInstances;

    protected override void Awake()
    {
        base.Awake();

        ClientObjects = new NetworkList<ClientObject>(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        ClientObjects.OnListChanged += ClientObjectsChanged;
        ClientObjects.Initialize(this);
    }

    void FixedUpdate()
    {
        if (!NetworkManager.IsListening || NetcodeUtils.IsClient)
        { return; }

        IReadOnlyList<ulong> clients = NetworkManager.ConnectedClientsIds;
        foreach (ulong clientId in clients)
        {
            if (!TryGetClientObject(clientId, out _))
            {
                ClientObjectInstances.Add(InstantiatePlayerObject(ClientObjectPrefab, clientId));
            }
        }

        if (!TryGetClientObject(NetworkManager.ServerClientId, out _))
        {
            ClientObjectInstances.Add(InstantiatePlayerObject(ClientObjectPrefab, NetworkManager.ServerClientId));
        }
    }

    void ClientObjectsChanged(NetworkListEvent<ClientObject> e)
    {
        if (!NetcodeUtils.IsClient)
        { return; }
    }

    bool TryGetClientObject(ulong clientId, out ulong objectId)
    {
        for (int i = ClientObjects.Count - 1; i >= 0; i--)
        {
            if (ClientObjects[i].ClientId != clientId) continue;

            objectId = ClientObjects[i].ClientId;
            return true;
        }

        objectId = default;
        return false;
    }

    void SetClientObject(ulong clientId, ulong objectId)
    {
        if (NetcodeUtils.IsClient)
        {
            Debug.LogError($"[{nameof(GameManager)}]: This should be not called on clients");
            return;
        }

        for (int i = ClientObjects.Count - 1; i >= 0; i--)
        {
            if (ClientObjects[i].ClientId != clientId) continue;

            ClientObject temp = ClientObjects[i];
            temp.ObjectId = objectId;
            ClientObjects[i] = temp;

            break;
        }

        ClientObjects.Add(new ClientObject(clientId, objectId));

        TakeControlManager.Instance.ShouldAlwaysControl(clientId, objectId, true);
    }

    GameObject InstantiatePlayerObject(GameObject prefab, ulong ownerId)
    {
        if (NetcodeUtils.IsClient)
        {
            Debug.LogError($"[{nameof(GameManager)}]: This should be not called on clients");
            return null;
        }

        GameObject instance = GameObject.Instantiate(prefab, ObjectGroups.Game);
        instance.SpawnOverNetwork();
        SetClientObject(ownerId, instance.GetComponent<NetworkObject>().NetworkObjectId);
        return instance;
    }
}
