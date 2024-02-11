using UnityEngine;

namespace Game.Components
{
    public class UnitBehaviour : MonoBehaviour
    {
        [SerializeField, Button(nameof(SetBehaviors), true, true, "Set Behaviors")] string btn_0;
        [SerializeField, ReadOnly, NonReorderable] UnitBehaviour_Base[] Behaviors = new UnitBehaviour_Base[0];

        [Header("Debug")]
        [SerializeField] bool ShowGizmos;

        public Vector2 GetOutput()
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

        void SetBehaviors()
        {
            Behaviors = GetComponents<UnitBehaviour_Base>();
            System.Array.Sort(Behaviors);
        }

        void Awake()
        {
            SetBehaviors();
        }

        void OnDrawGizmos()
        {
            for (int i = 0; i < Behaviors.Length; i++)
            {
                Behaviors[i].DrawGizmos();
            }
        }
    }
}
