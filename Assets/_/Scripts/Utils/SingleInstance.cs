using UnityEngine;

public class SingleInstance<T> : PrivateSingleInstance<T> where T : SingleInstance<T>
{
    internal static T Instance => instance;
}

public class PrivateSingleInstance<T> : MonoBehaviour where T : PrivateSingleInstance<T>
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
