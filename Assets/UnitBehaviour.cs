using System.Collections.Generic;

using Unity.Profiling;

using UnityEngine;
using UnityEngine.Profiling;

public class UnitBehaviour : MonoBehaviour
{
    [SerializeField, Button(nameof(SetBehaviours), true, true, "Set Behaviours")] string btn_0;
    [SerializeField, ReadOnly, NonReorderable] UnitBehaviour_Base[] Behaviours = new UnitBehaviour_Base[0];

    public static readonly ProfilerCategory ProfilerCategory = new("Game");
    public static readonly ProfilerMarker ProfilerMarker = new(ProfilerCategory, "Units.Behaviour", Unity.Profiling.LowLevel.MarkerFlags.Default);

    class Comparer : IComparer<UnitBehaviour_Base>
    {
        public int Compare(UnitBehaviour_Base x, UnitBehaviour_Base y)
            => x.CompareTo(y);
    }

    internal Vector2 GetOutput()
    {
        using (ProfilerMarker.Auto())
        {
            Vector2 result = Vector2.zero;

            for (int i = 0; i < Behaviours.Length; i++)
            {
                var subresult = Behaviours[i].GetOutput();
                if (!subresult.HasValue) continue;
                result = subresult.Value;
                break;
            }

            return result;
        }
    }

    void SetBehaviours()
    {
        Behaviours = GetComponents<UnitBehaviour_Base>();
        System.Array.Sort(Behaviours, new Comparer());
    }

    void Start()
    {
        Profiler.SetCategoryEnabled(ProfilerCategory, true);
        SetBehaviours();
    }
}
