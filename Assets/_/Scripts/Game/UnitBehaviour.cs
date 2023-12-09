using System.Collections.Generic;

using UnityEngine;

namespace Game.Components
{
    public class UnitBehaviour : MonoBehaviour
    {
        [SerializeField, Button(nameof(SetBehaviours), true, true, "Set Behaviors")] string btn_0;
        [SerializeField, ReadOnly, NonReorderable] UnitBehaviour_Base[] Behaviors = new UnitBehaviour_Base[0];

        class Comparer : IComparer<UnitBehaviour_Base>
        {
            public int Compare(UnitBehaviour_Base x, UnitBehaviour_Base y)
                => x.CompareTo(y);
        }

        internal Vector2 GetOutput()
        {
            Vector2 result = default;

            using (ProfilerMarkers.UnitsBehavior.Auto())
            {
                for (int i = 0; i < Behaviors.Length; i++)
                {
                    Vector2? subresult = Behaviors[i].GetOutput();
                    if (!subresult.HasValue) continue;
                    result = subresult.Value;
                    break;
                }
            }

            return result;
        }

        void SetBehaviours()
        {
            Behaviors = GetComponents<UnitBehaviour_Base>();
            System.Array.Sort(Behaviors, new Comparer());
        }

        void Awake()
        {
            SetBehaviours();
        }
    }
}
