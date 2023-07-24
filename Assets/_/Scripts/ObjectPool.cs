using System;
using System.Collections.Generic;

using UnityEngine;

public class PooledObject
{
    readonly int Index;

    public PooledObject(int index)
    {
        Index = index;
    }

    public GameObject Instantiate()
        => ObjectPool.Instance.Generate(Index);

    public GameObject Instantiate(Transform parent)
    {
        GameObject instance = Instantiate();
        if (instance == null) return null;

        instance.transform.SetParent(parent);
        return instance;
    }

    public GameObject Instantiate(Vector3 position, Quaternion rotation)
    {
        GameObject instance = Instantiate();
        if (instance == null) return null;

        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    public GameObject Instantiate(Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject instance = Instantiate();
        if (instance == null) return null;

        instance.transform.SetParent(parent);
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    internal bool TryGetComponent<T>(out T component) where T : Component
        => ObjectPool.Instance.Pools[Index].Prefab.TryGetComponent(out component);
}

public class ObjectPool : SingleInstance<ObjectPool>
{
    public class Pool
    {
        internal readonly GameObject Prefab;
        readonly List<GameObject> Instances;
        readonly int MaxSize;

        public Pool(GameObject prefab, int maxSize = 16)
        {
            Prefab = prefab;
            Instances = new List<GameObject>();
            MaxSize = maxSize;
        }

        public GameObject Generate()
        {
            for (int i = Instances.Count - 1; i >= 0; i--)
            {
                if (Instances[i] == null)
                {
                    Debug.LogWarning($"[{nameof(ObjectPool)}.{nameof(Pool)}]: Pooled instance is destroyed  (pool: \"{Prefab.name}\")", ObjectPool.instance);
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
                Debug.LogError($"[{nameof(ObjectPool)}.{nameof(Pool)}]: Size exeeded (pool: \"{Prefab.name}\" count: {Instances.Count})", ObjectPool.instance);
                return null;
            }

            GameObject newInstance = GameObject.Instantiate(Prefab);
            Instances.Add(newInstance);
            return newInstance;
        }
    }

    internal List<Pool> Pools = new();

    public PooledObject GeneratePool(GameObject prefab)
    {
        for (int i = 0; i < Pools.Count; i++)
        {
            if (Pools[i].Prefab == prefab)
            { return new PooledObject(i); }
        }
        Pools.Add(new Pool(prefab));
        return new PooledObject(Pools.Count - 1);
    }

    public GameObject Generate(int i)
        => Pools[i].Generate();
}
