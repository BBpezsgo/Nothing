using UnityEngine;

namespace Game.Components
{
    public class TeamThingSpawner : ThingSpawner
    {
        [SerializeField] string Team;

        protected override GameObject Spawn()
        {
            GameObject newThing = GameObject.Instantiate(Thing, GetPosition(), Quaternion.identity, transform);

            if (newThing.TryGetComponent(out BaseObject @object))
            { @object.Team = Team; }

            newThing.SpawnOverNetwork();
            return newThing;
        }
    }
}
