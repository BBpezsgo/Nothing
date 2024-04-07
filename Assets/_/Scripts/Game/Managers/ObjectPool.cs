using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#nullable enable

public readonly struct PooledPrefab : IEquatable<PooledPrefab>, IEquatable<PooledPrefab?>
{
    readonly int PoolIndex;
    readonly bool IsNotNull;
    readonly bool IsNull => !IsNotNull;

    public PooledPrefab(int index)
    {
        PoolIndex = index;
        IsNotNull = true;
    }

    public override bool Equals(object? obj) => obj is PooledPrefab other && Equals(other);

    public bool Equals(PooledPrefab other)
    {
        if (IsNull)
        { return other.IsNull; }

        return PoolIndex == other.PoolIndex;
    }

    public bool Equals(PooledPrefab? other)
    {
        if (!other.HasValue)
        { return IsNull; }

        return Equals(other.Value);
    }

    /// <exception cref="NullReferenceException"/>
    public void ThrowIfNull()
    {
        if (IsNull)
        { throw new NullReferenceException(); }
    }

    public override int GetHashCode() => HashCode.Combine(PoolIndex);
    public override string ToString() => IsNull ? "null" : $"{ObjectPool.Instance.Pools[PoolIndex].Prefab} Pool";

    /// <exception cref="NullReferenceException"/>
    public GameObject? Instantiate()
    {
        ThrowIfNull();

        return ObjectPool.Instance.Generate(PoolIndex);
    }

    /// <exception cref="NullReferenceException"/>
    public GameObject? Instantiate(Transform parent)
    {
        ThrowIfNull();

        GameObject? instance = Instantiate();
        if (instance == null) return null;

        instance.transform.SetParent(parent);
        return instance;
    }

    /// <exception cref="NullReferenceException"/>
    public GameObject? Instantiate(Vector3 position, Quaternion rotation, Transform? parent = null)
    {
        ThrowIfNull();

        GameObject? instance = Instantiate();
        if (instance == null) return null;

#pragma warning disable UNT0029 // Pattern matching with null on Unity objects
        if (parent is not null)
        { instance.transform.SetParent(parent); }
#pragma warning restore UNT0029
        instance.transform.SetPositionAndRotation(position, rotation);

        if (instance.TryGetComponent(out Rigidbody rigidbody))
        {
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.position = position;
            rigidbody.velocity = Vector3.zero;
            rigidbody.rotation = rotation;
            rigidbody.PublishTransform();
        }

        return instance;
    }

    /// <exception cref="NullReferenceException"/>
    public bool TryGetComponent<T>(out T component) where T : Component
    {
        ThrowIfNull();

        return ObjectPool.Instance.Pools[PoolIndex].Prefab.TryGetComponent(out component);
    }

    public static bool operator ==(PooledPrefab left, PooledPrefab right) => left.Equals(right);
    public static bool operator !=(PooledPrefab left, PooledPrefab right) => !left.Equals(right);

    public static bool operator ==(PooledPrefab left, PooledPrefab? right) => left.Equals(right);
    public static bool operator !=(PooledPrefab left, PooledPrefab? right) => !left.Equals(right);
}

public class ObjectPool : SingleInstance<ObjectPool>
{
    public readonly struct Pool
    {
        public readonly GameObject Prefab;
        readonly List<GameObject> Instances;
        readonly int MaxSize;

        public Pool(GameObject prefab, int maxSize = 16)
        {
            Prefab = prefab;
            Instances = new List<GameObject>();
            MaxSize = maxSize;
        }

        public GameObject? Instantiate()
        {
            for (int i = Instances.Count - 1; i >= 0; i--)
            {
                if (Instances[i] == null)
                {
                    Debug.LogWarning($"[{nameof(ObjectPool)}.{nameof(Pool)}]: Pooled instance is destroyed  (pool: \"{Prefab}\")", ObjectPool.instance);
                    Instances.RemoveAt(i);
                    continue;
                }

                if (!Instances[i].activeInHierarchy)
                {
                    GameObject instance = Instances[i];
                    instance.SetActive(true);
                    return instance;
                }
            }

            if (Instances.Count >= MaxSize)
            {
                if (ObjectPool.Instance.SizeExceedWarnings)
                { Debug.LogWarning($"[{nameof(ObjectPool)}.{nameof(Pool)}]: Size exceeded (pool: \"{Prefab}\" count: {Instances.Count})", ObjectPool.instance); }
                return null;
            }

            GameObject newInstance = GameObject.Instantiate(Prefab);
            Instances.Add(newInstance);
            return newInstance;
        }
    }

    public readonly List<Pool> Pools = new();
    public bool SizeExceedWarnings = true;

    /// <exception cref="ArgumentNullException"/>
    public PooledPrefab GeneratePool([NotNull] GameObject? prefab)
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));

        for (int i = 0; i < Pools.Count; i++)
        {
            if (Pools[i].Prefab == prefab)
            { return new PooledPrefab(i); }
        }

        Pools.Add(new Pool(prefab));
        return new PooledPrefab(Pools.Count - 1);
    }

    public GameObject? Generate(int i)
        => (i < 0 || i >= Pools.Count) ? null : Pools[i].Instantiate();
}
