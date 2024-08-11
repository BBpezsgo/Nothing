using System;
using UnityEngine;

namespace Game.Components
{
    public class ItemLoot : MonoBehaviour
    {
        [Serializable]
        internal class Loot
        {
            [SerializeField] internal string ItemID;
            [SerializeField] internal Vector2Int Amount = Vector2Int.one;
            [SerializeField, Range(0.00001f, 1f)] internal float Probability = 1f;

            internal int Evaluate()
            {
                float evaluatedProbability = UnityEngine.Random.value;

                if (evaluatedProbability < Probability)
                { return 0; }

                if (Amount.x == Amount.y)
                { return Amount.x; }

                int evaluatedAmount = UnityEngine.Random.Range(Math.Min(Amount.x, Amount.y), Math.Max(Amount.x, Amount.y));

                return evaluatedAmount;
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
