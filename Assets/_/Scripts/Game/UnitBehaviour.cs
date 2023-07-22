using System.Collections.Generic;

using UnityEngine;

namespace Game.Components
{
    public class UnitBehaviour : MonoBehaviour
    {
        [SerializeField, Button(nameof(SetBehaviours), true, true, "Set Behaviours")] string btn_0;
        [SerializeField, ReadOnly, NonReorderable] UnitBehaviour_Base[] Behaviours = new UnitBehaviour_Base[0];

        class Comparer : IComparer<UnitBehaviour_Base>
        {
            public int Compare(UnitBehaviour_Base x, UnitBehaviour_Base y)
                => x.CompareTo(y);
        }

        internal Vector2 GetOutput()
        {
            Vector2 result = Vector2.zero;

            using (ProfilerMarkers.UnitsBehaviour.Auto())
            {
                for (int i = 0; i < Behaviours.Length; i++)
                {
                    Vector2? subresult = Behaviours[i].GetOutput();
                    if (!subresult.HasValue) continue;
                    result = subresult.Value;
                    break;
                }
            }

            return result;
        }

        void SetBehaviours()
        {
            Behaviours = GetComponents<UnitBehaviour_Base>();
            System.Array.Sort(Behaviours, new Comparer());
        }

        void Start()
        {
            SetBehaviours();
        }
    }
}
