using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Game.Components
{
    public class ThingSpawner : NetworkBehaviour
    {
        [SerializeField, ReadOnly] Terrain Terrain;
        [SerializeField, NonReorderable, ReadOnly] List<GameObject> things = new();
        internal IReadOnlyList<GameObject> Things => things;

        [Header("Auto Spawn")]
        [SerializeField] bool EnableAutoSpawn;
        [SerializeField] protected GameObject Thing;
        [SerializeField, Min(0)] int MinThings = 1;
        [SerializeField, Min(0f)] float Cooldown = 1f;

        [SerializeField, ReadOnly] float NextSpawn = 1f;

        [Header("Properties")]
        [SerializeField] Vector2 Area;

        void Awake()
        {
            Terrain = FindFirstObjectByType<Terrain>();
        }

        void Update()
        {
            if (!NetcodeUtils.IsOfflineOrServer) return;

            things.PurgeObjects();

            if (!EnableAutoSpawn)
            { return; }

            if (things.Count >= MinThings)
            { return; }

            if (NextSpawn > 0f)
            {
                NextSpawn -= Time.deltaTime;
                return;
            }

            NextSpawn = Cooldown;

            GameObject newThing = Spawn(Thing);
            things.Add(newThing);
        }

        protected Vector3 GetPosition()
        {
            Vector3 position = new(Random.Range(-Area.x, Area.x), 0f, Random.Range(-Area.y, Area.y));
            position += transform.position;
            position.y = Terrain.SampleHeight(position) + Terrain.transform.position.y;
            position.y += 2f;
            return position;
        }

        internal virtual GameObject Spawn(GameObject prefab)
        {
            if (!NetcodeUtils.IsOfflineOrServer) return null;

            GameObject newThing = GameObject.Instantiate(prefab, GetPosition(), Quaternion.identity, transform);
            newThing.SpawnOverNetwork(true);
            return newThing;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(Area.x, 5f, Area.y));
        }
    }
}
