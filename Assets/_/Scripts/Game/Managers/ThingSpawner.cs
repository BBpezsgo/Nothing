using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace Game.Components
{
    public class ThingSpawner : NetworkBehaviour
    {
        [SerializeField, ReadOnly] Terrain Terrain;
        [SerializeField] protected GameObject Thing;

        [SerializeField, NonReorderable, ReadOnly] List<GameObject> Things = new();

        [SerializeField, Min(0)] int MinThings = 1;
        [SerializeField, ReadOnly] float NextSpawn = 1f;

        [SerializeField] Vector2 Area;

        void Start()
        {
            Terrain = FindObjectOfType<Terrain>();
        }

        void FixedUpdate()
        {
            if (!NetcodeUtils.IsOfflineOrServer)
            { return; }

            Things.PurgeObjects();

            if (Things.Count >= MinThings)
            { return; }

            if (NextSpawn > 0f)
            {
                NextSpawn -= Time.fixedDeltaTime;
                return;
            }

            TrySpawn();
        }

        void TrySpawn()
        {
            if (NextSpawn > 0f)
            { return; }
            NextSpawn = 1f;

            GameObject newThing = Spawn();
            Things.Add(newThing);
        }

        protected Vector3 GetPosition()
        {
            Vector3 position = new Vector3(Random.Range(-Area.x, Area.x), 0f, Random.Range(-Area.y, Area.y));
            position += transform.position;
            position.y = Terrain.SampleHeight(position) + Terrain.transform.position.y;
            position.y += 2f;
            return position;
        }

        protected virtual GameObject Spawn()
        {
            GameObject newThing = GameObject.Instantiate(Thing, GetPosition(), Quaternion.identity, transform);
            newThing.SpawnOverNetwork();
            return newThing;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(Area.x, 5f, Area.y));
        }
    }
}
