using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleNetworkInstance<T> : PrivateSingleNetworkInstance<T> where T : SingleNetworkInstance<T>
{
    internal static T Instance => instance;
}

public class PrivateSingleNetworkInstance<T> : Unity.Netcode.NetworkBehaviour where T : PrivateSingleNetworkInstance<T>
{
    protected static T instance;

    protected virtual void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning($"[{typeof(T).Name}]: Instance already registered, destroying self", gameObject);
            Object.Destroy(this);
            return;
        }
        instance = (T)this;
    }
}
