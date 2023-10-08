using System;
using Unity.Netcode;
using UnityEngine;

public static partial class UnclassifiedExtensions
{
    public static T Get<T>(this NetworkList<T> v, int index, T @default) where T : unmanaged, IEquatable<T>
    {
        if (index < 0 || index >= v.Count)
        { return @default; }
        return v[index];
    }
    public static void Set<T>(this NetworkList<T> v, int index, T value, T @default) where T : unmanaged, IEquatable<T>
    {
        if (index < 0)
        { return; }

        int endlessSafe = 16;
        while (index >= v.Count)
        {
            if (endlessSafe-- <= 0)
            {
                Debug.LogError($"Endless loop");
                return;
            }

            v.Add(@default);
        }

        v[index] = value;
    }

    public static void SpawnOverNetwork(this GameObject gameObject, bool destroyWithScene = true)
    {
        if (NetworkManager.Singleton == null)
        { return; }

        if (!NetworkManager.Singleton.IsListening)
        { return; }

        if (!gameObject.TryGetComponent(out NetworkObject networkObject))
        { return; }

        if (networkObject.IsSpawned)
        { return; }

        networkObject.Spawn(destroyWithScene);
    }
}

public static partial class ListEx
{
    public static void Enqueue<T>(this NetworkList<T> v, T element) where T : unmanaged, IEquatable<T>
    {
        if (v is null) throw new ArgumentNullException(nameof(v));

        v.Add(element);
    }

    public static T[] ToArray<T>(this NetworkList<T> v) where T : unmanaged, IEquatable<T>
    {
        if (v is null) throw new ArgumentNullException(nameof(v));

        T[] result = new T[v.Count];
        for (int i = 0; i < result.Length; i++)
        { result[i] = v[i]; }
        return result;
    }

    public static T Dequeue<T>(this NetworkList<T> v) where T : unmanaged, IEquatable<T>
    {
        if (v is null) throw new ArgumentNullException(nameof(v));

        T element = v[0];
        v.RemoveAt(0);
        return element;
    }
}
