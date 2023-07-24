using UnityEngine;

public class SingleInstance<T> : MonoBehaviour where T : SingleInstance<T>
{
    protected static T instance;

    internal static T Instance => instance;

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
