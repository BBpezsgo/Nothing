using System;
using UnityEngine;

namespace Game.Components
{
    public class ItemLoot : MonoBehaviour
    {
        [System.Serializable]
        internal class Loot
        {
            [SerializeField] internal string ItemID;
            [SerializeField] internal Vector2Int Ammount = Vector2Int.one;
            [SerializeField, Range(0.00001f, 1f)] internal float Propability = 1f;

            internal int Evaluate()
            {
                float evaulatedPropability = UnityEngine.Random.value;

                if (evaulatedPropability < Propability)
                { return 0; }

                if (Ammount.x == Ammount.y)
                { return Ammount.x; }

                int evaulatedAmmount = UnityEngine.Random.Range(Math.Min(Ammount.x, Ammount.y), Math.Max(Ammount.x, Ammount.y));

                return evaulatedAmmount;
            }
        }

        [SerializeField] Loot[] Loots = new Loot[0];

        internal void DropLoots()
        {
            for (int i = 0; i < Loots.Length; i++)
            {
                Loot loot = Loots[i];
                int dropCount = loot.Evaluate();
                if (dropCount <= 0)
                { continue; }

                if (!Managers.ItemManager.TryGetItem(loot.ItemID, out var item))
                {
                    Debug.LogWarning($"[{nameof(ItemLoot)}]: Item \"{loot.ItemID}\" not found", this);
                    continue;
                }

                for (int j = 0; j < dropCount; j++)
                {
                    GameObject instance = item.Instantiate(transform.position, transform.rotation);
                    if (this.TryGetComponent(out Rigidbody rigidbody1) &&
                        instance.TryGetComponent(out Rigidbody rigidbody2))
                    {
                        rigidbody2.linearVelocity = rigidbody1.linearVelocity;
                        rigidbody2.angularVelocity = rigidbody1.angularVelocity;
                    }
                }
            }
        }
    }
}
