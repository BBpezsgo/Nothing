using UnityEngine;

public class SingleInstance<T> : PrivateSingleInstance<T> where T : SingleInstance<T>
{ internal static T Instance => instance; }

public class PrivateSingleInstance<T> : MonoBehaviour where T : PrivateSingleInstance<T>
{
    protected static T instance;

    protected virtual void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning($"[{typeof(T).Name}]: Instance already registered, destroying self");
            Object.Destroy(this);
            return;
        }
        instance = (T)this;
    }
}
