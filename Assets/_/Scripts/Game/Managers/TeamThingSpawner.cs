using UnityEngine;

namespace Game.Components
{
    public class TeamThingSpawner : ThingSpawner
    {
        [SerializeField] string Team;

        internal override GameObject Spawn(GameObject prefab)
        {
            GameObject newThing = GameObject.Instantiate(prefab, GetPosition(), Quaternion.identity, transform);

            if (newThing.TryGetComponent(out BaseObject @object))
            { @object.Team = Team; }

            newThing.SpawnOverNetwork();
            return newThing;
        }
    }
}
