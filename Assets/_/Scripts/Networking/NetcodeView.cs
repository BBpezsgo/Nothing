using Messages;

using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Netcode/Netcode View")]
[DisallowMultipleComponent]
public class NetcodeView : MonoBehaviour
{
    [SerializeField, Button("UpdateComponents", true, true, "Update Components")] string _updateComponentsButton;
    /// <summary>
    /// <b>Only for debugging!</b>
    /// </summary>
    [SerializeField, ReadOnly] Component[] _observedComponents = new Component[0];
    [SerializeField, ReadOnly] uint _id = 0;
    [SerializeField, ReadOnly] NetcodeSynchronizer netcodeSynchronizer;
    [SerializeField, ReadOnly] internal bool IsRegistered = false;

    public uint ID { get => _id; set => _id = value; }
    INetworkObservable[] observedComponents = new INetworkObservable[0];
    public INetworkObservable[] ObservedNetworkComponents => observedComponents;
    public Component[] ObservedComponents => System.Array.ConvertAll(observedComponents, v => (Component)v);

    void Start() => UpdateThings();
    void OnEnable() => UpdateThings();

    void OnDestroy()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        { return; }
        if (netcodeSynchronizer != null)
        { netcodeSynchronizer.DestroyObject(this.ID); }
    }

    void UpdateThings()
    {
        UpdateComponents();
        netcodeSynchronizer = FindObjectOfType<NetcodeSynchronizer>();
        // if (netcodeSynchronizer == null) Debug.LogWarning($"{nameof(NetcodeSynchronizer)} not found");
        if (ID == 0 &&
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening &&
            netcodeSynchronizer != null)
        {
            netcodeSynchronizer.RegisterObjectInstance(gameObject, gameObject.name, transform.position, true);
            // Debug.LogError($"NetworkID is not set on object {this}", this);
        }
    }

    public void UpdateComponents()
    {
        observedComponents = GetComponentsInChildren<INetworkObservable>();
        _observedComponents = ObservedComponents;
    }

    internal void OnRpcRecived(RpcHeader header)
    {
        if (header.ComponentIndex >= observedComponents.Length)
        {
            Debug.LogError($"Component {header.ComponentIndex} not found in object {header.ObjectID}");
            return;
        }
        observedComponents[header.ComponentIndex].OnRPC(header);
    }

    internal void Despawn() => GameObject.Destroy(gameObject);
}
