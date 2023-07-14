using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

public class CreatureSpawner : NetworkBehaviour
{
    [SerializeField, ReadOnly] Terrain Terrain;
    [SerializeField] GameObject Creature;

    [SerializeField, NonReorderable, ReadOnly] List<GameObject> Creatures = new();

    [SerializeField, Min(0)] int MinCreatures = 1;
    [SerializeField, ReadOnly] float NextSpawn = 1f;

    [SerializeField] Vector2 Area;

    void Start()
    {
        Terrain = FindObjectOfType<Terrain>();
    }

    void FixedUpdate()
    {
        if (!this.IsOfflineOrServer())
        { return; }

        Creatures.PurgeObjects();

        if (Creatures.Count >= MinCreatures)
        { return; }

        if (NextSpawn > 0f)
        {
            NextSpawn -= Time.fixedDeltaTime;
            return;
        }

        Spawn();
    }

    void Spawn()
    {
        if (NextSpawn > 0f)
        { return; }
        NextSpawn = 1f;

        Vector3 position = new Vector3(Random.Range(-Area.x, Area.x), 0f, Random.Range(-Area.y, Area.y));
        position += transform.position;
        position.y = Terrain.SampleHeight(position) + Terrain.transform.position.y;
        position.y += 2f;

        GameObject newCreature = GameObject.Instantiate(Creature, position, Quaternion.identity, transform);
        newCreature.SpawnOverNetwork();
        
        Creatures.Add(newCreature);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(Area.x, 5f, Area.y));
    }
}
