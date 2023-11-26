using UnityEngine;

namespace Game.Managers
{
    public class ItemManager : PrivateSingleInstance<ItemManager>
    {
        [System.Serializable]
        internal class Item
        {
            [SerializeField] internal string ID;
            [SerializeField] internal GameObject Prefab;

            internal GameObject Instantiate() => Instantiate(Vector3.zero, Quaternion.identity, ObjectGroups.Items);
            internal GameObject Instantiate(Vector3 position) => Instantiate(position, Quaternion.identity, ObjectGroups.Items);
            internal GameObject Instantiate(Vector3 position, Quaternion rotation) => Instantiate(position, rotation, ObjectGroups.Items);
            internal GameObject Instantiate(Transform parent) => Instantiate(Vector3.zero, Quaternion.identity, parent);
            internal GameObject Instantiate(Vector3 position, Transform parent) => Instantiate(position, Quaternion.identity, parent);
            internal GameObject Instantiate(Vector3 position, Quaternion rotation, Transform parent)
            {
                GameObject instance = GameObject.Instantiate(Prefab, position, rotation, parent);

                if (instance.TryGetComponent(out Game.Components.Item item))
                { item.ItemID = ID; }
                else
                { Debug.LogError($"[{nameof(ItemManager)}.{nameof(Item)}]: Item prefab \"{ID}\" does not have a {nameof(Game.Components.Item)} component", instance); }

                return instance;
            }
        }

        [SerializeField] Item[] Items = new Item[0];

        void Validate()
        {
            if (Items == null)
            { return; }

            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i] == null)
                { continue; }

                if (Items[i].Prefab == null)
                { continue; }

                if (!Items[i].Prefab.HasComponent<Components.Item>())
                { Debug.LogError($"[{nameof(ItemManager)}.{nameof(Item)}]: Item prefab \"{Items[i].ID}\" does not have a {nameof(Components.Item)} component", this); }
            }
        }

        void Start()
        { Validate(); }

        void OnValidate()
        { Validate(); }

        internal static bool TryGetItem(string id, out Item item)
        {
            for (int i = 0; i < instance.Items.Length; i++)
            {
                if (instance.Items[i].ID != id)
                { continue; }

                item = instance.Items[i];
                return true;
            }
            item = null;
            return false;
        }

        internal static Item GetItem(string id)
            => TryGetItem(id, out Item item) ? item : null;
    }
}
